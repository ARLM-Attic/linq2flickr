<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="LinqFlickr_Demo._Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title>Flickr Photo Viewer</title>
    <link href="default.css" rel="stylesheet" type="text/css" />
    <style type="text/css">
        .style1
        {
            width: 100%;
        }
        .style2
        {
            width: 10px;
        }
    </style>
</head>
<body id="header">
    <form id="form1" runat="server">
        <div class="header_wrapper">
        <table style="width:100%;">
        <tr>
        <td>
            <img alt="Flickr Logo" src="flickr_logo.gif" style="width: 98px;height: 26px" /></td>
        <td align="right">
            
           <table>
           
            <tr>
                <td>
                </td>
                <td>
                    <asp:TextBox ID="textboxSearch" runat="server"></asp:TextBox>
                </td>
                <td style="font-size:small;">
                    <asp:RadioButton ID="checkSearchText" Text="free text" runat="server" GroupName="Mode" Checked="true" />
                    <asp:RadioButton ID="checkSearchTags" Text="tags only" runat="server" GroupName="Mode" />
                </td>
                <td>
                    <asp:Button ID="buttonSearch" runat="server" Text="Search" 
                onclick="buttonSearch_Click"  />
        
                </td>
            </tr>
            
         </table> 
        </td>
        </tr>
        </table>
        </div>
        
        <div   style="font-size:small;display:block;color:Blue;padding-right:5px;text-transform:lowercase;font-family:Sans-Serif;padding-bottom:10px;display:none;">
            <span >&nbsp;View Mode&nbsp;</span>
            <asp:RadioButton ID="rbPublic" runat="server" GroupName="vsb" Text="Public"  
                    AutoPostBack="true" oncheckedchanged="rbPublic_CheckedChanged"  />
            <asp:RadioButton ID="rbMeOnly" runat="server" Text="Only Me"  GroupName="vsb" 
                    Checked="true" AutoPostBack="true" oncheckedchanged="rbMeOnly_CheckedChanged" />
        
        
        </div>
        <asp:Panel ID="errorPanel" runat="server" Visible="false" CssClass="error_msg"  EnableViewState="false"><asp:Label ID="lblStatus" runat="server">Error is here</asp:Label> </asp:Panel>
        <div id="Main">
        <table style="border:0" cellpadding="0" cellspacing="0">
        
        <tr >
        <td valign="top">
            
         <asp:panel ID="detailView" runat="server">
              <table style="width:100%;">
              <tr>
              <td align="left">
                  <table class="style1">
                      <tr>
                          <td>
                              <asp:Label ID="lblTitle" runat="server" Font-Bold="True" Font-Size="X-Large" ForeColor="Black"></asp:Label>
                              <asp:HiddenField ID="hPhotoId" runat="server" />
                          </td>
                          <td align="right">
                              <asp:LinkButton ID="lnkDelete" runat="server" CommandName="delete" 
                                  onclick="lnkDelete_Click">[x] Delete</asp:LinkButton>
                          </td>
                      </tr>
                  </table>
             </td>
             <td>&nbsp;</td>
                  <td>
                  </td>
             </tr>
             <tr >
             <td style="width:505px;padding-right:20px;border-right:1px dashed #cccccc;" valign="top" >
             <div >
                <asp:Image runat="server" ID="photoDetail"  />
             </div>
             <div id="About">
                  <div class="photoDescription" >
                    <asp:Label ID="lblDescription" runat="server" ForeColor="Black"></asp:Label>
                 </div>
             </div>
             </td> 
             <td valign="top">
                 &nbsp;
                 <table class="style2" style="width:15px;">
                     <tr>
                         <td>
                             &nbsp;</td>
                     </tr>
               
                 </table>
                 &nbsp;</td>
                 <td valign="top">
                     <table cellpadding="0" cellspacing="0" class="style1">
                         <tr>
                             <td valign="top">
                                 <asp:Panel ID="nomarlView" runat="server" 
                                     style="width:240px;padding-left:5px;padding-top:2px;">
                                     <asp:DataList ID="lstPhotos" runat="server" 
                                         onitemcommand="lstPhotos_ItemCommand" onitemdatabound="lstPhotos_ItemDataBound" 
                                         RepeatColumns="3" Width="238">
                                         <ItemTemplate>
                                             <asp:LinkButton ID="lnkImage" runat="server" BorderWidth="0" 
                                                 CommandName="showDetail">
                                             <asp:Image ID="photo" runat="server" Height="75" style="border:solid 1px #ccc" 
                                                 Width="75" />
                                             </asp:LinkButton>
                                         </ItemTemplate>
                                     </asp:DataList>
                                 </asp:Panel>
                             </td>
                         </tr>
                         <tr>
                             <td>
                                 <div class="TagList" id="tagsDiv" runat="server">
                                    <br />
                                     <h4>
                                         Tags</h4>
                                     <asp:Repeater ID="lstTags" runat="server" 
                                         onitemdatabound="lstTags_ItemDataBound">
                                         <ItemTemplate>
                                             <img border="0" class="globe" height="16" src="icon_globe.gif" 
                                                 style="vertical-align:middle;" width="16" />&nbsp;<asp:HyperLink ID="lnkTagSrc" 
                                                 runat="server">Test</asp:HyperLink>
                                             <br />
                                         </ItemTemplate>
                                     </asp:Repeater>
                                 </div>
                             </td>
                         </tr>
                     </table>
                 </td>
                  </tr>
                  <tr>
                      <td align="left">
                          </td>
                          <td>
                            
                              &nbsp;</td>
                      <td>
                      </td>
                  </tr>
            </table>
         </asp:panel>
         </td>
         <td  align="center" valign="top">
         
             &nbsp;</td>
        </tr>
        </table>
       
       <br />
    
    <asp:Panel ID="panelUpload" runat="server" Visible="false">
    <fieldset>
      <legend>Upload Phtoto</legend>
          <table>
          <tr>
          <Td>Title :</Td> <td><asp:TextBox ID="txtTitle" runat="server" Width="278px"></asp:TextBox>  </td>
          </tr>
           <tr><td>Path:</td><td><input id="uploader" type="file" runat="server" />
            <asp:Button ID="btnUpload" runat="server" onclick="btnUpload_Click" 
                Text="Upload" /></td></tr>  
             <tr><td>Public:</td><td><asp:CheckBox ID="chkPublic" runat="server" /></td></tr> 
           </table>
           
           </fieldset>
     </asp:Panel>    
       </div>
    </form>
</body>
</html>
