<%@ Page Language="VB" ContentType="text/html" %>
<%@ Import Namespace="System" %>
<%@ Import Namespace="System.Web" %>

<script runat="server">
    Protected Function GetDumpHtml() As String
        Dim certHeaders As String() = {
            "HTTP_SVLJ_SUBJECT",
            "HTTP_SVLJ_ISSUER",
            "HTTP_SVLJ_SERIAL",
            "HTTP_SVLJ_VALIDFROM",
            "HTTP_SVLJ_VALIDTO",
            "HTTP_SVLJ_SIGNATUREALG"
        }

        Dim sb As New System.Text.StringBuilder()

	sb.Append("<strong>Misc information</strong><br>")
        sb.Append("Date & Time = " & DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") & "<br>")
        sb.Append("Remote Adress = " & Server.HtmlEncode(Request.ServerVariables("REMOTE_ADDR")) & "<br>")
        sb.Append("User Agent = " & Server.HtmlEncode(Request.UserAgent) & "<br>")
	sb.Append("<br>")

	sb.Append("<strong>Client Certificate information</strong><br>")
        For Each key As String In certHeaders
            Dim label As String = key.Replace("HTTP_SVLJ_", "")
            Dim value As String = Request.ServerVariables(key)
            If Not String.IsNullOrEmpty(value) Then
                sb.Append(Server.HtmlEncode(label) & " = " & Server.HtmlEncode(value) & "<br>")
            Else
                sb.Append(Server.HtmlEncode(label) & " = (not present)<br>")
            End If
        Next

        Return "<p>" & sb.ToString() & "</p>"
    End Function
</script>

<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <meta http-equiv="refresh" content="120" />
    <title>mTLSValidator Debugger</title>
</head>
<body>
     <h2>mTLSValidator Debugger</h2>
     <%= GetDumpHtml() %>
</body>
</html>
