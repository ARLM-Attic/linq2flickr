﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using Linq.Flickr.Interface;
using Linq.Flickr.Repository;

namespace Linq.Flickr.Authentication.Providers
{
    public class DesktopProvider : AuthenticaitonProvider
    {
        public override bool SaveToken(string permission)
        {
            IRepositoryBase repositoryBase = new BaseRepository();

            try
            {
                const string method = "flickr.auth.getToken";
                string frob = repositoryBase.GetFrob();

                string sig = repositoryBase.GetSignature(method, true, "frob", frob);
                string requestUrl = BuildUrl(method, "frob", frob, "api_sig", sig);

                DoTokenRequest(permission, frob);

                XmlElement tokenElement = GetElement(requestUrl);

                /// save everything to disc.
                OnAuthenticationComplete(GetAToken(tokenElement));
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to complete the authentication process", ex);
            }
            return true;
        }

        public override void OnAuthenticationComplete(AuthToken token)
        {
            FileStream stream = null;
            XmlWriter xmlWriter = null;

            try
            {
                string path = string.Format(baseDirectory + "token_{0}.xml", token.Perm);

                string xml = XmlToObject<AuthToken>.Serialize(token);

                stream = File.Open(path, FileMode.OpenOrCreate);

                var xmlWriterSettings = new XmlWriterSettings
                {
                    Indent = true, 
                    Encoding = System.Text.Encoding.UTF8
                };

                xmlWriter = XmlWriter.Create(stream, xmlWriterSettings);

                var document = new XmlDocument();

                document.LoadXml(xml);

                if (xmlWriter != null)
                {
                    document.Save(xmlWriter);
                }
            }
            finally
            {
                if (xmlWriter != null) xmlWriter.Close();
                if (stream != null) stream.Close();
            }
        }

        public override AuthToken GetToken(string permission)
        {
            StreamReader reader = null;
            try
            {
                string path = string.Format(baseDirectory + "token_{0}.xml", permission);

                reader = new StreamReader(path);

                return XmlToObject<AuthToken>.Deserialize(reader.ReadToEnd());

            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                if (reader != null) reader.Close();
            }
        }

        private void DoTokenRequest(string permission, string frob)
        {
            try
            {
                string authenticateUrl = GetAuthenticationUrl(permission, frob);
                // do process request and wait till the browser closes.
                Process p = new Process();
                p.StartInfo.FileName = "IExplore.exe";
                p.StartInfo.Arguments = authenticateUrl;
                p.Start();

                p.WaitForExit(int.MaxValue);

                if (p.HasExited)
                {
                    // do nothing.
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private AuthToken GetAToken(XmlElement tokenElement)
        {
            AuthToken token = (from tokens in tokenElement.Descendants("auth")
                               select new AuthToken
                               {
                                   Id = tokens.Element("token").InnerText ?? string.Empty,
                                   Perm = tokens.Element("perms").InnerText,
                                   UserId = tokens.Element("user").Attribute("nsid").Value ?? string.Empty,
                                   FullName = tokens.Element("user").Attribute("fullname").Value ?? string.Empty
                               }).Single<AuthToken>();

            return token;
        }

        public override void OnClearToken(AuthToken token)
        {
            string path = string.Format(baseDirectory + "token_{0}.xml", token.Perm);

            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        
  
        private readonly string baseDirectory = System.AppDomain.CurrentDomain.BaseDirectory;
    }
}
