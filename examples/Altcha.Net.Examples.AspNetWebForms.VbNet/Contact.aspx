<%@ Page Language="VB" AutoEventWireup="false" CodeBehind="Contact.aspx.vb" Inherits="Contact" %>

<!DOCTYPE html>
<html>
<head runat="server">
    <title>Contact</title>
    <script async defer src="/scripts/altcha.min.js" type="module"></script>
</head>
<body>
    <form id="ContactForm" runat="server">
        <asp:ValidationSummary runat="server" />
        <asp:Label ID="ErrorLabel" runat="server" ForeColor="Red" />
        <asp:Label ID="SuccessLabel" runat="server" ForeColor="Green" />

        <div>
            <asp:TextBox ID="EmailTextBox" runat="server" TextMode="Email" />
        </div>
        <div>
            <asp:TextBox ID="MessageTextBox" runat="server" TextMode="MultiLine" />
        </div>

        <altcha-widget challenge="/AltchaChallenge.ashx"></altcha-widget>

        <asp:Button ID="SubmitButton" runat="server" Text="Envoyer" />
    </form>
</body>
</html>
