Public Class FunctionModel
    Public Function Cleanse(sInput As String) As String
        Dim new_string As String = ""
        new_string = Trim(sInput).Replace("'", "!?1?!") ' Replacing ' with !?1?!
        Return new_string
    End Function

    Public Function Uncleanse(sInput As String) As String
        Dim new_string As String = ""
        new_string = Trim(sInput).Replace("!?1?!", "'") ' Replacing !?1?! with '
        Return new_string
    End Function

End Class
