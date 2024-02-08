Imports System.Data
Imports System.Web.Http
Imports MySql.Data.MySqlClient

Public Class AnimalsController
    Inherits ApiController

    Private ReadOnly connectionString As String = ConfigurationManager.ConnectionStrings("MySqlConnection").ConnectionString

    ' GET api/animals
    <Route("api/animals/getaallanimalstable")>
    Public Function GetAllAnimals() As IHttpActionResult

        Dim animals As New List(Of Animal)()

        Using connection As New MySqlConnection(connectionString)
            connection.Open()

            Dim query As String = "SELECT * FROM animals"
            Using cmd As New MySqlCommand(query, connection)
                Using reader As MySqlDataReader = cmd.ExecuteReader()
                    While reader.Read()
                        Dim animal As New Animal() With {
                            .Animal_Id = Convert.ToInt32(reader("animal_id")),
                            .Name = reader("name").ToString(),
                            .Type = reader("type").ToString(),
                            .Age = Convert.ToInt32(reader("age"))
                        }
                        animals.Add(animal)
                    End While
                End Using
            End Using
        End Using

        If animals.Count > 0 Then
            Return Ok(animals)
        Else
            Return NotFound()
        End If
    End Function

    'Get Caretakers
    <Route("api/animals/getaallcaretakersstable")>
    Public Function GetAllCaretakers() As IHttpActionResult
        Try
            Dim caretakers As New List(Of CaretakerModel)()

            Using connection As New MySqlConnection(connectionString)
                connection.Open()

                Dim query As String = "SELECT * FROM caretakers"

                Using cmd As New MySqlCommand(query, connection)
                    Using reader As MySqlDataReader = cmd.ExecuteReader()
                        While reader.Read()
                            Dim caretaker As New CaretakerModel() With {
                            .CaretakerId = Convert.ToInt32(reader("caretaker_id")),
                            .CaretakerName = reader("caretaker_name").ToString(),
                            .PhoneNumber = If(reader("phone_number") IsNot DBNull.Value, reader("phone_number").ToString(), Nothing),
                            .Email = If(reader("email") IsNot DBNull.Value, reader("email").ToString(), Nothing)
                        }
                            caretakers.Add(caretaker)
                        End While
                    End Using
                End Using
            End Using

            If caretakers.Count > 0 Then
                Return Ok(caretakers)
            Else
                Return NotFound()
            End If
        Catch ex As Exception
            Return InternalServerError(ex)
        End Try
    End Function

    'Get all animals that is below <=5
    <HttpGet, Route("api/animals/young")>
    Public Function GetAnimalsWithAgeLessThan5() As IHttpActionResult
        Dim animals As New List(Of Animal)()

        Using connection As New MySqlConnection(connectionString)
            connection.Open()

            Dim query As String = "SELECT * FROM animals WHERE age <= 5"
            Using cmd As New MySqlCommand(query, connection)
                Using reader As MySqlDataReader = cmd.ExecuteReader()
                    While reader.Read()
                        Dim animal As New Animal() With {
                        .Animal_Id = Convert.ToInt32(reader("animal_id")),
                        .Name = reader("name").ToString(),
                        .Type = reader("type").ToString(),
                        .Age = Convert.ToInt32(reader("age"))
                    }
                        animals.Add(animal)
                    End While
                End Using
            End Using
        End Using

        If animals.Count > 0 Then
            Return Ok(animals)
        Else
            Return NotFound()
        End If
    End Function
    ' GET api/animals/1
    Public Function GetAnimalById(id As Integer) As IHttpActionResult
        Using connection As New MySqlConnection(connectionString)
            connection.Open()

            Dim query As String = "SELECT * FROM animals WHERE animal_id = @id"
            Using cmd As New MySqlCommand(query, connection)
                cmd.Parameters.AddWithValue("@id", id)

                Using reader As MySqlDataReader = cmd.ExecuteReader()
                    If reader.Read() Then
                        Dim animal As New Animal() With {
                            .Animal_Id = Convert.ToInt32(reader("animal_id")),
                            .Name = reader("name").ToString(),
                            .Type = reader("type").ToString(),
                            .Age = Convert.ToInt32(reader("age"))
                        }
                        Return Ok(animal)
                    Else
                        Return NotFound()
                    End If
                End Using
            End Using
        End Using
    End Function



    ' POST api/animals

    <HttpPost>
    Public Function AddAnimals(<FromBody> animals As List(Of Animal)) As IHttpActionResult
        If animals Is Nothing OrElse animals.Count = 0 Then
            Return BadRequest("Invalid data. Animal list is null or empty.")
        End If

        Using connection As New MySqlConnection(connectionString)
            connection.Open()

            ' Start a transaction
            Using transaction As MySqlTransaction = connection.BeginTransaction()
                Try
                    For Each animal In animals
                        Dim query As String = "INSERT INTO animals (name, type, age) VALUES (@name, @type, @age)"
                        Using cmd As New MySqlCommand(query, connection, transaction)
                            cmd.Parameters.AddWithValue("@name", animal.Name)
                            cmd.Parameters.AddWithValue("@type", animal.Type)
                            cmd.Parameters.AddWithValue("@age", animal.Age)

                            cmd.ExecuteNonQuery()
                        End Using
                    Next

                    ' Commit the transaction if all insertions are successful
                    transaction.Commit()

                    Return Ok("Animals added successfully.")
                Catch ex As Exception
                    ' Rollback the transaction if there is an exception
                    transaction.Rollback()
                    Return InternalServerError(ex)
                End Try
            End Using
        End Using
    End Function


    ' PUT api/animals
    <HttpPut>
    Public Function UpdateAnimal(id As Integer, <FromBody> updatedAnimal As Animal) As IHttpActionResult
        If updatedAnimal Is Nothing Then
            Return BadRequest("Invalid data. Updated animal object is null.")
        End If

        Using connection As New MySqlConnection(connectionString)
            connection.Open()

            ' Check if the animal with the new ID already exists
            Dim checkQuery As String = "SELECT COUNT(*) FROM animals WHERE animal_id = @newId"
            Using checkCmd As New MySqlCommand(checkQuery, connection)
                checkCmd.Parameters.AddWithValue("@newId", updatedAnimal.Animal_Id)
                Dim existingCount As Integer = Convert.ToInt32(checkCmd.ExecuteScalar())

                ' If the new ID already exists and is not the same as the old ID, return conflict
                If existingCount > 0 AndAlso updatedAnimal.Animal_Id <> id Then
                    Return Conflict()
                End If
            End Using

            ' Delete the existing record with the old ID
            Dim deleteQuery As String = "DELETE FROM animals WHERE animal_id = @id"
            Using deleteCmd As New MySqlCommand(deleteQuery, connection)
                deleteCmd.Parameters.AddWithValue("@id", id)
                deleteCmd.ExecuteNonQuery()
            End Using

            ' Insert a new record with the updated ID and data
            Dim insertQuery As String = "INSERT INTO animals (animal_id, name, type, age) VALUES (@newId, @name, @type, @age)"
            Using insertCmd As New MySqlCommand(insertQuery, connection)
                insertCmd.Parameters.AddWithValue("@newId", updatedAnimal.Animal_Id)
                insertCmd.Parameters.AddWithValue("@name", updatedAnimal.Name)
                insertCmd.Parameters.AddWithValue("@type", updatedAnimal.Type)
                insertCmd.Parameters.AddWithValue("@age", updatedAnimal.Age)
                insertCmd.ExecuteNonQuery()
            End Using

            Return Ok($"Animal with ID {id} updated to {updatedAnimal.Animal_Id} successfully.")
        End Using
    End Function

    'Delete api/animals
    <HttpDelete>
    Public Function DeleteAnimal(id As Integer) As IHttpActionResult
        Using connection As New MySqlConnection(connectionString)
            connection.Open()

            ' Check if the animal with the specified ID exists
            Dim checkQuery As String = "SELECT COUNT(*) FROM animals WHERE animal_id = @id"
            Using checkCmd As New MySqlCommand(checkQuery, connection)
                checkCmd.Parameters.AddWithValue("@id", id)
                Dim existingCount As Integer = Convert.ToInt32(checkCmd.ExecuteScalar())

                If existingCount = 0 Then
                    Return NotFound()
                End If
            End Using

            ' Delete the animal with the specified ID
            Dim deleteQuery As String = "DELETE FROM animals WHERE animal_id = @id"
            Using deleteCmd As New MySqlCommand(deleteQuery, connection)
                deleteCmd.Parameters.AddWithValue("@id", id)
                deleteCmd.ExecuteNonQuery()
            End Using

            Return Ok($"Animal with ID {id} deleted successfully.")
        End Using
    End Function
    'Delete data with higher or equal than 5 
    <Route("api/animals/HigherQ5")>
    <HttpDelete>
    Public Function DeleteAnimalsGreaterThanEqualFive() As IHttpActionResult
        Using connection As New MySqlConnection(connectionString)
            connection.Open()

            ' Delete animals with ID greater than or equal to 5
            Dim deleteQuery As String = "DELETE FROM animals WHERE animal_id >= 5"
            Using deleteCmd As New MySqlCommand(deleteQuery, connection)
                deleteCmd.ExecuteNonQuery()
            End Using

            Return Ok("Animals with ID greater than or equal to 5 deleted successfully.")
        End Using
    End Function

End Class

