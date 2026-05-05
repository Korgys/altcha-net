Imports System

Public Class Contact
    Inherits System.Web.UI.Page

    Protected Sub SubmitButton_Click(sender As Object, e As EventArgs) Handles SubmitButton.Click
        Dim result = AltchaProvider.Service.ValidateResponse(Request.Form("altcha"))

        If Not result.IsValid Then
            ErrorLabel.Text = "Validation ALTCHA invalide."
            SuccessLabel.Text = ""
            Return
        End If

        ErrorLabel.Text = ""
        SuccessLabel.Text = "Message envoye."
    End Sub
End Class
