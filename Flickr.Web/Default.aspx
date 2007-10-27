<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Flickr.Web._Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title>Untitled Page</title>
</head>
<body>
    <form id="form1" runat="server">
        <table style="background-color:Black;border:0;color:White;" cellpadding="0" cellspacing="0">
       
        <tr>
        <td  align="center">
        <asp:Panel ID="nomarlView" runat="server" style="width:240px;padding-left:5px;padding-top:2px;">
        <asp:Repeater ID="lstPhotos" runat="server"  
                onitemdatabound="lstPhotos_ItemDataBound" 
                onitemcommand="lstPhotos_ItemCommand" >
            <ItemTemplate>
                <div style="display:inline;float:left;padding:1px;text-align:center;" >
                    <asp:LinkButton ID="lnkImage" runat="server" BorderWidth="0" CommandName="showDetail">
                        <asp:Image runat="server" ID="photo" BorderColor="white" />
                    </asp:LinkButton>
                </div>
             </ItemTemplate>
        </asp:Repeater>
         </asp:Panel>
         <asp:panel ID="detailView" runat="server" Visible="false">
              <table style="width:100%;">
              <tr>
              <td align="left" style="background-color:White;">
             <asp:LinkButton ID="lnkBack" runat="server" CommandName="back" 
                      onclick="lnkBack_Click"> Back</asp:LinkButton>
                  &nbsp;<asp:LinkButton ID="lnkDelete" runat="server" CommandName="delete" onclick="lnkDelete_Click" 
                      >Delete</asp:LinkButton>
                 <asp:HiddenField ID="hPhotoId" runat="server" />     
             </td>
             </tr>
             <tr><td><asp:Image runat="server" ID="photoDetail" BorderColor="white" /></td></tr>
                  <tr>
                      <td>
                          &nbsp;
                      </td>
                  </tr>
            </table>
         </asp:panel>
        </td>
        </tr>
        </table>
       
       <br />
       <h4 style="border-bottom:solid 1px #ccc;text-transform:uppercase;">Upload photo</h4>
            
           
           <input id="uploader" type="file" runat="server" />
            <asp:Button ID="btnUpload" runat="server" onclick="btnUpload_Click" 
                Text="Upload" />
           
    </form>
</body>
</html>
