Imports System.Net
Imports System.Web.Http
Imports MySql.Data.MySqlClient

Namespace Controllers
    Public Class ClockTimeController
        Inherits ApiController

        ' Replace with your actual connection string
        Private ReadOnly connectionString As String = ConfigurationManager.ConnectionStrings("MySqlConnection").ConnectionString

        ' GET api/ClockTime/GetAllClockTime
        <Route("api/clocktime/getallclocktime")>
        Public Function GetAllClockTime() As IHttpActionResult
            Try
                Using connection As New MySqlConnection(connectionString)
                    connection.Open()

                    Dim query As String = "SELECT * FROM ClockTime"
                    Using command As New MySqlCommand(query, connection)
                        Using adapter As New MySqlDataAdapter(command)
                            Dim dataTable As New DataTable()
                            adapter.Fill(dataTable)

                            ' Convert DataTable to a list of ClockTime
                            Dim clockTimeList As New List(Of ClockTime)()
                            For Each row As DataRow In dataTable.Rows
                                Dim clockTime As New ClockTime() With {
                                    .Date = row("Date").ToString(),
                                    .TimeIn = row("TimeIn").ToString(),
                                    .TimeOut = row("TimeOut").ToString(),
                                    .TotalShift = row("TotalShift").ToString(),
                                    .Overtime = row("Overtime").ToString()
                                }
                                clockTimeList.Add(clockTime)
                            Next

                            Return Ok(clockTimeList)
                        End Using
                    End Using
                End Using
            Catch ex As Exception
                ' Handle exceptions appropriately (logging, returning an error response, etc.)
                Return InternalServerError(ex)
            End Try
        End Function
    End Class
End Namespace
