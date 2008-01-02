<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Flickr.Web._Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title>Flickr Test App</title>
    <link href="default.css" rel="stylesheet" type="text/css" />
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
        
        <div  style="font-size:small;display:block;color:Blue;padding-right:5px;text-transform:lowercase;font-family:Sans-Serif;padding-bottom:10px;">
            <span >&nbsp;View Mode&nbsp;</span>
            <asp:RadioButton ID="rbPublic" runat="server" GroupName="vsb" Text="Public"  
                    AutoPostBack="true" oncheckedchanged="rbPublic_CheckedChanged"  />
            <asp:RadioButton ID="rbMeOnly" runat="server" Text="Only Me"  GroupName="vsb" 
                    Checked="true" AutoPostBack="true" oncheckedchanged="rbMeOnly_CheckedChanged" />
        
        
        </div>
        <asp:Panel ID="errorPanel" runat="server" Visible="false" CssClass="error_msg"  EnableViewState="false"><asp:Label ID="lblStatus" runat="server">Error is here</asp:Label> </asp:Panel>
        <table style="background-color:Black;border:0;color:White;" cellpadding="0" cellspacing="0">
        <tr>
            <TD>
            
            </TD>
        </tr>
        <tr>
        <td  align="center">
        <asp:Panel ID="nomarlView" runat="server" style="width:240px;padding-left:5px;padding-top:2px;height:325px;">
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
              <td align="left" style="background-color:Black;color:White;">
             <asp:LinkButton ID="lnkBack" runat="server" CommandName="back" 
                      onclick="lnkBack_Click" ForeColor="White"> &lt;&lt;Back</asp:LinkButton>
                  &nbsp;<asp:LinkButton ID="lnkDelete" runat="server" CommandName="delete" ForeColor="White" onclick="lnkDelete_Click" 
                      >[x] Delete</asp:LinkButton>
                 <asp:HiddenField ID="hPhotoId" runat="server" />     
             </td>
             </tr>
             <tr ><td style="width:505px;height:377px;"><asp:Image runat="server" ID="photoDetail" BorderColor="white" /></td></tr>
                  <tr>
                      <td align="left">
                           <i>Tags</i>: <asp:Label ID="lblTags" Text="(n/a)" runat="server" ForeColor="White"></asp:Label>
                          </td>
                  </tr>
            </table>
         </asp:panel>
        </td>
        </tr>
        </table>
       
       <br />
    <asp:Panel ID="panelUpload" runat="server" Visible="false">
    
       <h4 style="border-bottom:solid 1px #ccc;text-transform:uppercase;">Upload photo</h4>
          <table>
          <tr>
          <Td>Title :</Td> <td><asp:TextBox ID="txtTitle" runat="server"></asp:TextBox>  </td>
          </tr>
           <tr><td>Path:</td><td><input id="uploader" type="file" runat="server" />
            <asp:Button ID="btnUpload" runat="server" onclick="btnUpload_Click" 
                Text="Upload" /></td></tr>  
             <tr><td>Public:</td><td><asp:CheckBox ID="chkPublic" runat="server" /></td></tr> 
           </table>
     </asp:Panel>      
    </form>
</body>
</html>
