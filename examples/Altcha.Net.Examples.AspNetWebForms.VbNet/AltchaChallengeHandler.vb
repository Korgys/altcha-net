Imports System.Web

Public Class AltchaChallengeHandler
    Implements IHttpHandler

    Public Sub ProcessRequest(context As HttpContext) Implements IHttpHandler.ProcessRequest
        context.Response.ContentType = "application/json"
        context.Response.Write(AltchaProvider.Service.GenerateChallenge().ToJson())
    End Sub

    Public ReadOnly Property IsReusable As Boolean Implements IHttpHandler.IsReusable
        Get
            Return True
        End Get
    End Property
End Class
