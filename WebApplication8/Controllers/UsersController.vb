Imports System.Net
Imports System.Net.Http
Imports System.Web.Http
Imports MySql.Data.MySqlClient
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq

Namespace Controllers
    Public Class UsersController
        Inherits ApiController


        Private ReadOnly connectionString As String = ConfigurationManager.ConnectionStrings("MySqlConnection").ConnectionString

        <Route("api/users/getallUsers")>
        Public Function GetAllUsers() As IHttpActionResult
            Try
                Using connection As New MySqlConnection(connectionString)
                    connection.Open()

                    Dim query As String = "SELECT * FROM users"
                    Using command As New MySqlCommand(query, connection)
                        Using adapter As New MySqlDataAdapter(command)
                            Dim dataTable As New DataTable()
                            adapter.Fill(dataTable)


                            Dim usersList As New List(Of Users)()
                            For Each row As DataRow In dataTable.Rows
                                Dim user As New Users() With {
                                    .UserId = Convert.ToInt32(row("User_Id")),
                                    .Username = row("username").ToString(),
                                    .Email = row("email").ToString(),
                                    .PasswordHash = row("password_hash").ToString(),
                                    .LoginDate = Convert.ToDateTime(row("login_date"))
                                }
                                usersList.Add(user)
                            Next

                            Return Ok(usersList)
                        End Using
                    End Using
                End Using
            Catch ex As Exception

                Return InternalServerError(ex)
            End Try
        End Function



        <Route("api/Users/InsertUser")>
        <HttpPost>
        Public Function InsertUser()
            Dim response As HttpResponseMessage
            response = Request.CreateResponse(HttpStatusCode.Created)
            Dim requestBody As String = Request.Content.ReadAsStringAsync().Result
            Dim requestData = JsonConvert.DeserializeObject(requestBody)

            If requestData("Username") = " " Then
                Return Ok("Username is required")
            ElseIf requestData("Email") = "" Then
                Return Ok("Email is requiered")
            ElseIf requestData("Password") = "" Then
                Return Ok("Password is required")
            End If

            Try
                Using connection As New MySqlConnection(connectionString)
                    connection.Open()


                    Dim checkQuery As String = $"SELECT username,email FROM users WHERE " &
                                        $"username = '{requestData("Username")}' OR " &
                                        $"email = '{requestData("Email")}'"

                    Using checkCmd As New MySqlCommand(checkQuery, connection)
                        checkCmd.Parameters.AddWithValue("@Username", requestData("Username"))
                        checkCmd.Parameters.AddWithValue("@Email", requestData("Email"))


                        Using reader As MySqlDataReader = checkCmd.ExecuteReader()
                            If reader.HasRows Then

                                Return BadRequest("Username or Email already in use.")
                            End If
                        End Using
                    End Using


                    Dim insertQuery As String = "INSERT INTO users (username, email, password_hash, login_date,UserStatus) VALUES (@Username, @Email, AES_ENCRYPT(@Password, 'YourEncryptionKey'), NOW(), 'Active');"

                    Using cmd As New MySqlCommand(insertQuery, connection)
                        cmd.Parameters.AddWithValue("@Username", requestData("Username"))
                        cmd.Parameters.AddWithValue("@Email", requestData("Email"))
                        cmd.Parameters.AddWithValue("@Password", requestData("Password"))
                        cmd.ExecuteNonQuery()
                    End Using
                End Using

                Return Ok("User data inserted successfully.")
            Catch ex As Exception
                Return InternalServerError(ex)
            End Try
        End Function





        <Route("api/Users/Login")>
        <HttpPost>
        Public Function Login()
            Dim response As HttpResponseMessage
            response = Request.CreateResponse(HttpStatusCode.OK)

            Try

                Dim requestBody As String = Request.Content.ReadAsStringAsync().Result
                Dim requestData = JsonConvert.DeserializeObject(requestBody)


                If requestData("EmailUS") = "" Then
                    Return Ok("Username or Email is required")
                ElseIf requestData("Password") = "" Then
                    Return Ok("Password is required")
                End If

                Using connection As New MySqlConnection(connectionString)
                    connection.Open()


                    Dim loginQuery As String = $"SELECT COUNT(*) FROM users WHERE " &
                                      $"(username = '{requestData("EmailUS")}' OR email = '{requestData("EmailUS")}') AND " &
                                      $"password_hash = AES_ENCRYPT('{requestData("Password")}', 'YourEncryptionKey')"

                    Using cmd As New MySqlCommand(loginQuery, connection)

                        Dim count As Integer = Convert.ToInt32(cmd.ExecuteScalar())

                        If count > 0 Then

                            Return Ok("Login successful")
                        Else

                            Return Ok("Login failed")
                        End If
                    End Using
                End Using
            Catch ex As Exception

                Return InternalServerError(ex)
            End Try
        End Function



        <Route("api/Users/CreatePost")>
        <HttpPost>
        Public Function CreatePost() As IHttpActionResult
            Try

                Dim requestBody As String = Request.Content.ReadAsStringAsync().Result


                Dim requestData As JObject = JsonConvert.DeserializeObject(Of JObject)(requestBody)

                If requestData("user_id") Is Nothing OrElse requestData("user_id").ToString() = "" Then
                    Return BadRequest("UserId is required")
                ElseIf requestData("Caption") Is Nothing OrElse requestData("Caption").ToString() = "" Then
                    Return BadRequest("Caption is required")
                ElseIf requestData("Location") Is Nothing OrElse requestData("Location").ToString() = "" Then
                    Return BadRequest("Location is required")
                End If

                Using connection As New MySqlConnection(connectionString)
                    connection.Open()


                    Dim checkQuery As String = "SELECT COUNT(*) FROM users WHERE user_id = @UserId"
                    Using checkCmd As New MySqlCommand(checkQuery, connection)
                        checkCmd.Parameters.AddWithValue("@UserId", requestData("user_id").ToString())
                        Dim count As Integer = Convert.ToInt32(checkCmd.ExecuteScalar())

                        If count = 0 Then
                            Return BadRequest("User ID not found.")
                        End If
                    End Using


                    Dim insertQuery As String = "INSERT INTO posts (Caption, Location, Post_date, user_id) VALUES (@Caption, @Location, NOW(), @user_id);"

                    Using cmd As New MySqlCommand(insertQuery, connection)
                        cmd.Parameters.AddWithValue("@Caption", requestData("Caption").ToString())
                        cmd.Parameters.AddWithValue("@Location", requestData("Location").ToString())

                        cmd.Parameters.AddWithValue("@Post_date", DateTime.Now)


                        Dim userId As Integer = Convert.ToInt32(requestData("user_id"))
                        cmd.Parameters.AddWithValue("@user_id", userId)

                        cmd.ExecuteNonQuery()
                    End Using


                End Using


                Return Ok("CreatePost data inserted successfully.")
            Catch ex As Exception

                Return InternalServerError(ex)
            End Try
        End Function





        <Route("api/Users/UpdateUser")>
        <HttpPost>
        Public Function UpdateUser()
            Dim response As HttpResponseMessage
            response = Request.CreateResponse(HttpStatusCode.OK)

            Dim requestBody As String = Request.Content.ReadAsStringAsync().Result
            Dim requestData = JsonConvert.DeserializeObject(requestBody)

            If requestData("Email") = "" OrElse requestData("NewUsername") = "" OrElse requestData("NewEmail") = "" OrElse requestData("NewPassword") = "" Then
                Return Ok("Email, NewUsername, NewEmail, and NewPassword are required for update.")
            End If

            Try
                Using connection As New MySqlConnection(connectionString)
                    connection.Open()


                    Dim checkQuery As String = $"SELECT username, email, password_hash FROM users WHERE " &
                            $"username = '{requestData("Username")}' AND " &
                            $"email = '{requestData("Email")}' AND " &
                            $"password_hash = AES_ENCRYPT('{requestData("Password")}', 'YourEncryptionKey');"

                    Using checkCmd As New MySqlCommand(checkQuery, connection)
                        Using reader As MySqlDataReader = checkCmd.ExecuteReader()
                            If Not reader.HasRows Then

                                Return BadRequest("User not found for update.")
                            End If
                        End Using
                    End Using


                    Dim updateQuery As String = "UPDATE users SET username = @NewUsername, email = @NewEmail, password_hash = AES_ENCRYPT(@NewPassword, 'YourEncryptionKey') WHERE email = @Email;"

                    Using cmd As New MySqlCommand(updateQuery, connection)
                        cmd.Parameters.AddWithValue("@Email", requestData("Email"))
                        cmd.Parameters.AddWithValue("@NewUsername", requestData("NewUsername"))
                        cmd.Parameters.AddWithValue("@NewEmail", requestData("NewEmail"))
                        cmd.Parameters.AddWithValue("@NewPassword", requestData("NewPassword"))
                        cmd.ExecuteNonQuery()
                    End Using
                End Using

                Return Ok("User data updated successfully.")
            Catch ex As Exception
                Return InternalServerError(ex)
            End Try
        End Function






        <HttpPost>
        <Route("api/Users/UpdatePost")>
        Public Function UpdatePost()
            Dim response As HttpResponseMessage
            response = Request.CreateResponse(HttpStatusCode.OK)

            Dim requestBody As String = Request.Content.ReadAsStringAsync().Result
            Dim requestData = JsonConvert.DeserializeObject(requestBody)

            If requestData("UserId") Is Nothing OrElse requestData("UserId").ToString() = "" Then
                Return BadRequest("UserId is required")
            ElseIf requestData("Caption") Is Nothing OrElse requestData("Caption").ToString() = "" Then
                Return BadRequest("Caption is required")
            ElseIf requestData("Location") Is Nothing OrElse requestData("Location").ToString() = "" Then
                Return BadRequest("Location is required")
            End If

            Try
                Using connection As New MySqlConnection(connectionString)
                    connection.Open()

                    Dim checkQuery As String = $"SELECT user_id FROM posts " &
                            $"WHERE user_id = {requestData("UserId")} AND " &
                            $"user_id IN (SELECT user_id FROM users);"


                    Using checkCmd As New MySqlCommand(checkQuery, connection)
                        Using reader As MySqlDataReader = checkCmd.ExecuteReader()
                            If Not reader.HasRows Then

                                Return BadRequest("User not found for update.")
                            End If
                        End Using
                    End Using


                    Dim updateQuery As String = "UPDATE posts SET Caption = '" & requestData("Caption") & "', Location = '" & requestData("Location") & "', Post_date = NOW() WHERE post_id = " & requestData("PostId") & ";"

                    Using cmd As New MySqlCommand(updateQuery, connection)
                        cmd.Parameters.AddWithValue("@Caption", requestData("Caption"))
                        cmd.Parameters.AddWithValue("@Location", requestData("Location"))
                        cmd.Parameters.AddWithValue("@UserId", requestData("UserId"))
                        cmd.Parameters.AddWithValue("@PostId", requestData("PostId"))
                        cmd.ExecuteNonQuery()
                    End Using


                End Using

                Return Ok("User data updated successfully.")
            Catch ex As Exception
                Return InternalServerError(ex)
            End Try
        End Function





        <HttpPost>
        <Route("api/Users/GetALLPost")>
        Public Function GetALLPost() As IHttpActionResult
            Try
                ' Retrieve the request body and deserialize it
                Dim requestBody As String = Request.Content.ReadAsStringAsync().Result
                Dim requestData = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(requestBody)

                If String.IsNullOrEmpty(requestData("UserId")) Then
                    Return BadRequest("User ID is required")
                End If

                Using connection As New MySqlConnection(connectionString)
                    connection.Open()


                    Dim getAllPostQuery As String = "SELECT * FROM posts WHERE user_id = @user_id AND Status = 'Active';"


                    Using cmd As New MySqlCommand(getAllPostQuery, connection)

                        cmd.Parameters.AddWithValue("@user_id", requestData("UserId"))


                        Dim reader As MySqlDataReader = cmd.ExecuteReader()

                        Dim postsList As New List(Of Object)()


                        While reader.Read()
                            Dim post As New With {
                                .post_id = reader("post_id"),
                                .Caption = reader("Caption"),
                                .Location = reader("Location"),
                                .Post_date = reader("Post_date"),
                                .user_id = reader("user_id")
                            }
                            postsList.Add(post)
                        End While


                        reader.Close()


                        Dim resultObject As New With {
                            .UserId = Integer.Parse(requestData("UserId")),
                            .Posts = postsList
                        }


                        Return Ok(resultObject)
                    End Using
                End Using
            Catch ex As Exception

                Return InternalServerError(ex)
            End Try
        End Function



        <Route("api/Users/DeletePost")>
        <HttpPost>
        Public Function DeletePost() As IHttpActionResult
            Try

                Dim requestBody As String = Request.Content.ReadAsStringAsync().Result


                Dim requestData As JObject = JsonConvert.DeserializeObject(Of JObject)(requestBody)

                If requestData("PostId") Is Nothing OrElse requestData("PostId").ToString() = "" Then
                    Return BadRequest("PostId is required")
                ElseIf requestData("UserId") Is Nothing OrElse requestData("UserId").ToString = "" Then
                    Return BadRequest("UserId is required")
                End If


                Using connection As New MySqlConnection(connectionString)
                    connection.Open()


                    Dim updateQuery As String = $"UPDATE posts SET Status = 'Deleted' WHERE post_id = {requestData("PostId")} AND user_id = {requestData("UserId")};" &
                          $"UPDATE notif SET notif_status = 'Inactive' WHERE reference_id = {Convert.ToInt32(requestData("PostId"))} AND sender_id = {Convert.ToInt32(requestData("UserId"))} AND notif_status <> 'Inactive';"





                    Using cmd As New MySqlCommand(updateQuery, connection)

                        Dim rowsAffected As Integer = cmd.ExecuteNonQuery()

                        If rowsAffected = 0 Then

                            Return BadRequest("Post not found for the given User ID and Post ID.")
                        End If
                    End Using
                End Using


                Return Ok("Post successfully marked as deleted")
            Catch ex As Exception

                Console.WriteLine($"Exception: {ex.Message}")

                Return InternalServerError(ex)
            End Try
        End Function


        <Route("api/Users/FollowPost")>
        <HttpPost>
        Public Function FollowPost() As IHttpActionResult
            Try
                Dim requestBody As String = Request.Content.ReadAsStringAsync().Result
                Dim requestData As JObject = JsonConvert.DeserializeObject(Of JObject)(requestBody)

                If IsNothing(requestData("UserId")) OrElse requestData("UserId").ToString().Trim() = "" Then
                    Return BadRequest("UserId request data required.")
                ElseIf IsNothing(requestData("FollowersId")) OrElse requestData("FollowersId").ToString().Trim() = "" Then
                    Return BadRequest("FollowersId request data required.")
                End If

                Using connection As New MySqlConnection(connectionString)
                    connection.Open()

                    ' Check user status before following
                    Dim checkUserStatusQuery As String = $"SELECT UserStatus FROM users WHERE user_id = {Convert.ToInt32(requestData("FollowersId"))};"

                    Using checkUserStatusCmd As New MySqlCommand(checkUserStatusQuery, connection)
                        Dim userStatus As Object = checkUserStatusCmd.ExecuteScalar()

                        If userStatus IsNot Nothing AndAlso userStatus.ToString() = "Inactive" Then
                            ' If the user status is inactive, throw an error
                            Return BadRequest("Account cannot be followed because it is currently in Deactivated mode")
                        End If
                    End Using

                    Using transaction = connection.BeginTransaction()
                        Try
                            Dim checkFollowQuery As String = $"SELECT * FROM ff WHERE Followers_id = {Convert.ToInt32(requestData("FollowersId"))} AND Followed_id = {Convert.ToInt32(requestData("UserId"))};"

                            Using checkFollowCmd As New MySqlCommand(checkFollowQuery, connection)
                                Dim existingFollow As Object = checkFollowCmd.ExecuteScalar()

                                If existingFollow IsNot Nothing Then
                                    Dim statusQuery As String = $"SELECT Status FROM ff WHERE Followers_id = {Convert.ToInt32(requestData("FollowersId"))} AND Followed_id = {Convert.ToInt32(requestData("UserId"))};"

                                    Using statusCmd As New MySqlCommand(statusQuery, connection)
                                        Dim currentStatus As Object = statusCmd.ExecuteScalar()

                                        If currentStatus IsNot Nothing AndAlso currentStatus.ToString() = "Inactive" Then
                                            Dim updateStatusQuery As String = $"UPDATE ff SET Status = 'Active' WHERE Followers_id = {Convert.ToInt32(requestData("FollowersId"))} AND Followed_id = {Convert.ToInt32(requestData("UserId"))};"

                                            Using updateStatusCmd As New MySqlCommand(updateStatusQuery, connection)
                                                updateStatusCmd.ExecuteNonQuery()
                                            End Using

                                            Dim updateStatusNotif As String = $"UPDATE notif SET notif_status = 'Active' WHERE sender_id = {Convert.ToInt32(requestData("UserId"))} AND reference_id = {Convert.ToInt32(requestData("UserId"))}  AND reference_type = 'Followed';"

                                            Using updateStatusCmd As New MySqlCommand(updateStatusNotif, connection)
                                                updateStatusCmd.ExecuteNonQuery()
                                            End Using

                                            transaction.Commit()
                                            Return Ok("Followed successfully.")
                                        Else
                                            Return BadRequest("Follow already exists.")
                                        End If
                                    End Using
                                End If
                            End Using

                            Dim insertQuery As String = $"INSERT INTO ff (Followers_id, Followed_id, Status) VALUES ({Convert.ToInt32(requestData("FollowersId"))}, {Convert.ToInt32(requestData("UserId"))}, 'Active');"

                            Using insertCmd As New MySqlCommand(insertQuery, connection)
                                insertCmd.ExecuteNonQuery()
                            End Using

                            Dim insertNotif As String = $"INSERT INTO notif (notif_content, sender_id, receiver_id, notif_date, reference_id, reference_type, notif_status) VALUES ('Followed You', {Convert.ToInt32(requestData("UserId"))}, {Convert.ToInt32(requestData("FollowersId"))}, NOW(), {Convert.ToInt32(requestData("UserId"))}, 'Followed', 'Active');"

                            Using insertCmd As New MySqlCommand(insertNotif, connection)
                                insertCmd.ExecuteNonQuery()
                            End Using

                            transaction.Commit()
                            Return Ok("Follow data inserted successfully.")
                        Catch ex As Exception
                            transaction.Rollback()
                            Throw
                        End Try
                    End Using
                End Using
            Catch ex As Exception
                Console.WriteLine(ex.ToString())
                Return InternalServerError(ex)
            End Try
        End Function




        <Route("api/Users/UnfollowPost")>
        <HttpPost>
        Public Function UnfollowPost() As IHttpActionResult
            Try

                Dim requestBody As String = Request.Content.ReadAsStringAsync().Result


                Dim requestData As JObject = JsonConvert.DeserializeObject(Of JObject)(requestBody)

                If IsNothing(requestData("UserId")) OrElse requestData("UserId").ToString().Trim() = "" Then
                    Return BadRequest("UserId request data required.")
                ElseIf IsNothing(requestData("FollowersId")) OrElse requestData("FollowersId").ToString().Trim() = "" Then
                    Return BadRequest("FollowersId request data required.")
                End If

                Using connection As New MySqlConnection(connectionString)
                    connection.Open()


                    Dim checkStatusQuery As String = $"SELECT Status FROM ff WHERE Followers_id = {Convert.ToInt32(requestData("FollowersId"))} AND Followed_Id = {Convert.ToInt32(requestData("UserId"))};"
                    Using checkCmd As New MySqlCommand(checkStatusQuery, connection)
                        Dim currentStatus As Object = checkCmd.ExecuteScalar()

                        If currentStatus IsNot Nothing AndAlso currentStatus.ToString().Trim().Equals("Inactive", StringComparison.OrdinalIgnoreCase) Then
                            Return Ok("Already Unfollowed")
                        End If
                    End Using


                    Dim updateQuery As String = $"UPDATE ff SET Status = 'Inactive' WHERE Followers_id = {Convert.ToInt32(requestData("FollowersId"))} AND Followed_Id = {Convert.ToInt32(requestData("UserId"))}"

                    Using cmd As New MySqlCommand(updateQuery, connection)
                        Dim rowsAffected As Integer = cmd.ExecuteNonQuery()

                        If rowsAffected = 0 Then

                            Return BadRequest("Post not found for the given User ID and Followers ID.")
                        End If
                    End Using


                    Dim updateNotifQuery As String = $"UPDATE notif SET notif_status = 'Inactive' WHERE sender_id = {Convert.ToInt32(requestData("UserId"))}  AND reference_type = 'Followed'"

                    Using notifCmd As New MySqlCommand(updateNotifQuery, connection)
                        Dim notifRowsAffected As Integer = notifCmd.ExecuteNonQuery()


                        If notifRowsAffected = 0 Then
                            Console.WriteLine("No notifications found for the given User ID.")
                        End If
                    End Using
                End Using


                Return Ok("Unfollow Successful")
            Catch ex As Exception

                Console.WriteLine($"Exception: {ex.Message}")

                Return InternalServerError(ex)
            End Try
        End Function







        <HttpPost>
        <Route("api/Users/GetFollowers")>
        Public Function GetFollowers() As IHttpActionResult
            Try

                Dim requestBody As String = Request.Content.ReadAsStringAsync().Result
                Dim requestData = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(requestBody)


                If String.IsNullOrEmpty(requestData("UserId")) Then
                    Return BadRequest("User ID is required")
                End If

                Using connection As New MySqlConnection(connectionString)
                    connection.Open()


                    Dim getFollowersQuery As String = "SELECT Followed_id FROM ff WHERE Followers_id = @user_id And Status = 'Active'  ;"

                    Using cmd As New MySqlCommand(getFollowersQuery, connection)

                        cmd.Parameters.AddWithValue("@user_id", requestData("UserId"))


                        Dim reader As MySqlDataReader = cmd.ExecuteReader()


                        Dim followersList As New List(Of Integer)()


                        While reader.Read()
                            followersList.Add(reader.GetInt32("Followed_id"))
                        End While

                        reader.Close()


                        Dim resultObject As New With {
                    .UserId = Integer.Parse(requestData("UserId")),
                    .Followers = followersList
                }


                        Return Ok(resultObject)
                    End Using
                End Using
            Catch ex As Exception

                Return InternalServerError(ex)
            End Try
        End Function



        <HttpPost>
        <Route("api/Users/GetFollowing")>
        Public Function GetFollowing() As IHttpActionResult
            Try

                Dim requestBody As String = Request.Content.ReadAsStringAsync().Result
                Dim requestData = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(requestBody)


                If String.IsNullOrEmpty(requestData("UserId")) Then
                    Return BadRequest("User ID is required")
                End If

                Using connection As New MySqlConnection(connectionString)
                    connection.Open()


                    Dim getFollowersQuery As String = "SELECT Followers_id FROM ff WHERE Followed_id = @user_id  And Status = 'Active';"


                    Using cmd As New MySqlCommand(getFollowersQuery, connection)

                        cmd.Parameters.AddWithValue("@user_id", requestData("UserId"))


                        Dim reader As MySqlDataReader = cmd.ExecuteReader()


                        Dim followedList As New List(Of Integer)()


                        While reader.Read()
                            followedList.Add(reader.GetInt32("Followers_id"))
                        End While


                        reader.Close()


                        Dim resultObject As New With {
                    .UserId = Integer.Parse(requestData("UserId")),
                    .Following = followedList
                }


                        Return Ok(resultObject)
                    End Using
                End Using
            Catch ex As Exception

                Return InternalServerError(ex)
            End Try
        End Function

        <HttpPost>
        <Route("api/Users/GetFollowingPost")>
        Public Function GetFollowingPost() As IHttpActionResult
            Try

                Dim requestBody As String = Request.Content.ReadAsStringAsync().Result
                Dim requestData = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(requestBody)


                If String.IsNullOrEmpty(requestData("UserId")) Then
                    Return BadRequest("User ID is required")
                End If

                Using connection As New MySqlConnection(connectionString)
                    connection.Open()


                    Dim getFollowersQuery As String = $"SELECT post_id, Caption, Location, Post_date, user_id FROM posts " &
                $"WHERE user_id IN (SELECT Followers_id FROM ff WHERE Followed_id = {requestData("UserId")} AND Status = 'Active') " &
                $"AND Status = 'Active';"

                    Using cmd As New MySqlCommand(getFollowersQuery, connection)

                        Dim reader As MySqlDataReader = cmd.ExecuteReader()


                        Dim postIdList As New List(Of Integer)()


                        Dim postList As New List(Of Object)()


                        While reader.Read()

                            Dim postId As Integer = reader.GetInt32("post_id")

                            postIdList.Add(postId)

                            Dim postObject = New With {
                        .UserId = reader.GetString("user_id"),
                        .PostId = postId,
                        .Caption = reader.GetString("Caption"),
                        .Location = reader.GetString("Location"),
                        .PostDate = reader.GetDateTime("Post_date"),
                        .CommentCount = 0,
                        .Comments = New List(Of Object)()}


                            postList.Add(postObject)
                        End While


                        reader.Close()


                        For Each postObject In postList

                            Dim getCommentCountQuery As String = $"SELECT COUNT(*) AS comment_count " &
                                                    $"FROM commenttbl " &
                                                    $"WHERE post_id = {postObject.PostId} AND status = 'Active';"

                            Using commentCountCmd As New MySqlCommand(getCommentCountQuery, connection)

                                postObject.CommentCount = Convert.ToInt32(commentCountCmd.ExecuteScalar())
                            End Using

                            Dim getCommentsQuery As String = $"SELECT c.post_id, c.user_id, u.username AS comment_username, c.comments, c.comment_date " &
                                            $"FROM commenttbl c " &
                                            $"JOIN users u ON c.user_id = u.user_id " &
                                            $"WHERE c.post_id = {postObject.PostId} AND c.status = 'Active';"


                            Using commentsCmd As New MySqlCommand(getCommentsQuery, connection)

                                Dim commentsReader As MySqlDataReader = commentsCmd.ExecuteReader()


                                Dim commentsList As New List(Of Object)()


                                While commentsReader.Read()

                                    Dim commentObject = New With {
                                .PostId = commentsReader.GetInt32("post_id"),
                                .UserId = commentsReader.GetString("user_id"),
                                .Username = commentsReader.GetString("comment_username"),
                                .Comments = commentsReader.GetString("comments"),
                                .CommentDate = commentsReader.GetDateTime("comment_date")
                            }


                                    commentsList.Add(commentObject)
                                End While


                                commentsReader.Close()


                                postObject.Comments = commentsList
                            End Using
                        Next

                        Dim resultObject As New With {
                    .UserId = Integer.Parse(requestData("UserId")),
                    .PostIds = postIdList,
                    .Posts = postList
                }


                        Return Ok(resultObject)
                    End Using
                End Using
            Catch ex As Exception

                Return InternalServerError(ex)
            End Try
        End Function








        <Route("api/Users/CommentPost")>
        <HttpPost>
        Public Function CommentPost()
            Try
                Dim requestBody As String = Request.Content.ReadAsStringAsync().Result
                Dim requestData As JObject = JsonConvert.DeserializeObject(Of JObject)(requestBody)

                If requestData("PostId") Is Nothing OrElse String.IsNullOrEmpty(requestData("PostId").ToString()) Then
                    Return BadRequest("PostId is required")
                ElseIf requestData("UserId") Is Nothing OrElse String.IsNullOrEmpty(requestData("UserId").ToString()) Then
                    Return BadRequest("UserId is required")
                ElseIf requestData("Comment") Is Nothing OrElse String.IsNullOrEmpty(requestData("Comment").ToString()) Then
                    Return BadRequest("Comment is required")
                End If

                Dim postId As Integer = Convert.ToInt32(requestData("PostId"))

                ' Check if the user is blocked by the author of the post
                Dim blockQuery As String = $"SELECT Blocker_id, Blocked_id, Block_status 
                             FROM block 
                             WHERE Blocker_id = {requestData("UserId")} 
                             AND Blocked_id = (SELECT user_id FROM posts WHERE post_id = {postId}) 
                             AND Block_status = 'Blocked';"

                ' Check if the author of the post is blocked by the user
                Dim reverseBlockQuery As String = $"SELECT Blocker_id, Blocked_id, Block_status 
                                    FROM block 
                                    WHERE Blocker_id = (SELECT user_id FROM posts WHERE post_id = {postId}) 
                                    AND Blocked_id = {requestData("UserId")}
                                    AND Block_status = 'Blocked';"

                ' Check if the author's account is inactive
                Dim userStatusQuery As String = $"SELECT UserStatus FROM users 
                                    WHERE user_id = (SELECT user_id FROM posts WHERE post_id = {postId});"

                Using connection As New MySqlConnection(connectionString)
                    connection.Open()

                    ' Execute blockQuery and reverseBlockQuery as before

                    ' Check user status
                    Using userStatusCmd As New MySqlCommand(userStatusQuery, connection)
                        Dim userStatus As String = Convert.ToString(userStatusCmd.ExecuteScalar())
                        If userStatus = "Inactive" Then
                            Return BadRequest("Comment cannot be posted because the account is deactivated")
                        End If
                    End Using

                    ' Cleansing the comment before insertion
                    Dim cleanse As New FunctionModel()
                    Dim cleansedComment As String = cleanse.Cleanse(requestData("Comment").ToString())

                    ' Insert the comment into the database
                    Dim insertQuery As String = $"INSERT INTO commenttbl (post_id, user_id, comments, comment_date, status) VALUES ({postId}, {requestData("UserId")}, '{cleansedComment}', NOW(), 'Active');"
                    Using cmd As New MySqlCommand(insertQuery, connection)
                        cmd.ExecuteNonQuery()

                        Dim commentId As Integer = Convert.ToInt32(cmd.LastInsertedId)

                        ' Insert a notification for the post author
                        Dim insertNotifQuery As String = $"INSERT INTO notif (notif_content, sender_id, receiver_id, notif_date, reference_id, reference_type, notif_status) 
                                                  VALUES ('Commented on your Post', {requestData("UserId")}, (SELECT user_id FROM posts WHERE post_id = {postId}), NOW(), {commentId}, 'Commented', 'Active');"
                        Using cmdInsertNotif As New MySqlCommand(insertNotifQuery, connection)
                            cmdInsertNotif.ExecuteNonQuery()
                        End Using
                    End Using
                End Using

                Return Ok("Comment inserted successfully.")
            Catch ex As Exception
                Return InternalServerError(ex)
            End Try
        End Function





        <Route("api/Users/DeleteComment")>
        <HttpPost>
        Public Function DeleteComment() As IHttpActionResult
            Try

                Dim requestBody As String = Request.Content.ReadAsStringAsync().Result

                Dim requestData As JObject = JsonConvert.DeserializeObject(Of JObject)(requestBody)

                If IsNothing(requestData("CommentId")) OrElse requestData("CommentId").ToString().Trim() = "" Then
                    Return BadRequest("CommentId request data required.")
                End If


                Using connection As New MySqlConnection(connectionString)
                    connection.Open()


                    Dim checkStatusQuery As String = $"SELECT comment_id FROM commenttbl WHERE comment_id = {requestData("CommentId").ToString()};"
                    Using checkCmd As New MySqlCommand(checkStatusQuery, connection)
                        Dim currentStatus As Object = checkCmd.ExecuteScalar()
                        If currentStatus Is Nothing Then
                            Return BadRequest("Comment ID not found.")
                        End If
                    End Using


                    Dim updateQuery As String = $"UPDATE commenttbl SET Status = 'Inactive' WHERE comment_id = {requestData("CommentId").ToString()} AND status = 'Active';" &
                                                $"UPDATE notif SET notif_status = 'Inactive' WHERE reference_id =  {requestData("CommentId").ToString()} AND notif_status = 'Active' AND reference_type = 'Commented';"

                    Using cmd As New MySqlCommand(updateQuery, connection)


                        Dim rowsAffected As Integer = cmd.ExecuteNonQuery()

                        If rowsAffected = 0 Then

                            Return BadRequest("Comment not found or already inactive.")
                        End If
                    End Using
                End Using


                Return Ok("Comment Deleted")
            Catch ex As Exception

                Console.WriteLine($"Exception: {ex.Message}")

                Return InternalServerError(ex)
            End Try
        End Function




        <HttpPost>
        <Route("api/Users/GetPostId")>
        Public Function GetPost()
            Dim response As HttpResponseMessage
            response = Request.CreateResponse(HttpStatusCode.Created)
            Try
                Dim requestBody As String = Request.Content.ReadAsStringAsync().Result
                Dim requestData = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(requestBody)

                Dim valid = True
                If IsNothing(requestData("PostId")) OrElse requestData("PostId").ToString().Trim() = "" Then
                    valid = False
                    response.StatusCode = HttpStatusCode.BadRequest
                    response.Content = New StringContent("Invalid request. user_id is required")
                End If

                If valid = False Then
                    Return response
                    Exit Function
                End If

                Using connection As New MySqlConnection(connectionString)
                    connection.Open()

                    ' Query to fetch block information
                    Dim blockQuery As String = "SELECT Blocker_id, Blocked_id, Block_status FROM block;"
                    Using blockCmd As New MySqlCommand(blockQuery, connection)
                        Dim blockReader As MySqlDataReader = blockCmd.ExecuteReader()

                        Dim blockList As New List(Of Object)()

                        While blockReader.Read()
                            Dim blockObject As New With {
                        .BlockerId = blockReader.GetInt32("Blocker_id"),
                        .BlockedId = blockReader.GetInt32("Blocked_id"),
                        .BlockStatus = blockReader.GetString("Block_status")
                    }
                            blockList.Add(blockObject)
                        End While

                        blockReader.Close()

                        ' Now, proceed with fetching post information
                        Dim getPostQuery As String = $"SELECT post_id, user_id, Caption, Location, Post_date FROM posts WHERE post_id = {Convert.ToInt32(requestData("PostId"))} AND Status = 'Active';"

                        Using cmd As New MySqlCommand(getPostQuery, connection)
                            Dim reader As MySqlDataReader = cmd.ExecuteReader()

                            If reader.HasRows Then
                                reader.Read()

                                Dim resultObject As New With {
                            .PostId = reader.GetInt32("post_id"),
                            .UserId = reader.GetInt32("user_id"),
                            .Caption = reader.GetString("Caption"),
                            .Location = reader.GetString("Location"),
                            .PostDate = reader.GetDateTime("Post_date"),
                            .CommentCount = 0,
                            .Comments = Nothing,
                            .BlockList = blockList ' Include block list in the result
                        }

                                reader.Close()

                                ' Fetch comment count
                                Dim getCommentCountQuery As String = $"SELECT COUNT(*) AS comment_count FROM commenttbl WHERE post_id = {Convert.ToInt32(requestData("PostId"))} AND status = 'Active';"

                                Using commentCountCmd As New MySqlCommand(getCommentCountQuery, connection)
                                    resultObject.CommentCount = Convert.ToInt32(commentCountCmd.ExecuteScalar())
                                End Using

                                ' Fetch comments
                                Dim getCommentsQuery As String = $"SELECT c.comment_id, c.post_id, c.user_id, u.username, c.comments, c.comment_date FROM commenttbl c LEFT JOIN users u ON c.user_id = u.user_id WHERE c.post_id = {Convert.ToInt32(requestData("PostId"))} AND status = 'Active' ORDER BY c.comment_date DESC;"

                                Using commentsCmd As New MySqlCommand(getCommentsQuery, connection)
                                    Dim commentsReader As MySqlDataReader = commentsCmd.ExecuteReader()

                                    If commentsReader.HasRows Then
                                        Dim commentsList As New List(Of Object)()

                                        While commentsReader.Read()
                                            ' Get comment information
                                            Dim commentId = commentsReader.GetInt32("comment_id")
                                            Dim postId = commentsReader.GetInt32("post_id")
                                            Dim userId = commentsReader.GetInt32("user_id")
                                            Dim username = commentsReader.GetString("username")
                                            Dim commentText = commentsReader.GetString("comments")
                                            Dim commentDate = commentsReader.GetDateTime("comment_date")

                                            ' Check if commenter is blocked by the post owner
                                            Dim isBlocked = False
                                            For Each blockObj In blockList
                                                If blockObj.BlockerId = resultObject.UserId AndAlso blockObj.BlockedId = userId AndAlso blockObj.BlockStatus = "Blocked" Then
                                                    username = "User" ' If blocked by post owner, change the username to 'User'
                                                    Exit For
                                                ElseIf blockObj.BlockerId = userId AndAlso blockObj.BlockedId = resultObject.UserId AndAlso blockObj.BlockStatus = "Blocked" Then
                                                    username = "User" ' If blocked by commenter, change the username to 'User'
                                                    Exit For
                                                End If
                                            Next


                                            ' Create comment object
                                            Dim commentObject As New With {
                                        .CommentId = commentId,
                                        .PostId = postId,
                                        .UserId = userId,
                                        .Username = username,
                                        .Comments = commentText,
                                        .CommentDate = commentDate
                                    }

                                            ' Add comment object to the list
                                            commentsList.Add(commentObject)
                                        End While

                                        commentsReader.Close()

                                        resultObject.Comments = commentsList
                                    End If
                                End Using

                                Return Ok(resultObject)
                            Else
                                Return NotFound()
                            End If
                        End Using
                    End Using
                End Using
            Catch ex As Exception
                Return InternalServerError(ex)
            End Try
        End Function



        <Route("api/Users/LikePost")>
        <HttpPost>
        Public Function LikePost() As IHttpActionResult
            Dim response As HttpResponseMessage = Request.CreateResponse(HttpStatusCode.Created)

            Try
                Dim requestBody As String = Request.Content.ReadAsStringAsync().Result
                Dim requestData As JObject = JsonConvert.DeserializeObject(Of JObject)(requestBody)

                Dim valid = True
                If IsNothing(requestData("PostId")) OrElse requestData("PostId").ToString().Trim() = "" Then
                    valid = False
                    response.StatusCode = HttpStatusCode.BadRequest
                    response.Content = New StringContent("Invalid request. PostId is required.")
                ElseIf IsNothing(requestData("UserId")) OrElse requestData("UserId").ToString().Trim() = "" Then
                    valid = False
                    response.StatusCode = HttpStatusCode.BadRequest
                    response.Content = New StringContent("Invalid request. UserId is required.")
                End If

                If Not valid Then
                    Return ResponseMessage(response)
                End If

                ' Check if the user is blocked by the author of the post
                Dim blockQuery As String = $"SELECT Blocker_id, Blocked_id, Block_status 
                                     FROM block 
                                     WHERE Blocker_id = {requestData("UserId")} 
                                     AND Blocked_id = (SELECT user_id FROM posts WHERE post_id = {requestData("PostId")}) 
                                     AND Block_status = 'Blocked';"

                ' Check if the author of the post is blocked by the user
                Dim reverseBlockQuery As String = $"SELECT Blocker_id, Blocked_id, Block_status 
                                            FROM block 
                                            WHERE Blocker_id = (SELECT user_id FROM posts WHERE post_id = {requestData("PostId")}) 
                                            AND Blocked_id = {requestData("UserId")}
                                            AND Block_status = 'Blocked';"

                Using connection As New MySqlConnection(connectionString)
                    connection.Open()

                    ' Check if the user is blocked by the author of the post
                    Using blockCmd As New MySqlCommand(blockQuery, connection)
                        Using reader As MySqlDataReader = blockCmd.ExecuteReader()
                            If reader.Read() Then
                                ' User is blocked by the author of the post, return an error
                                Return BadRequest("Something went wrong cannot like this post")
                            End If
                        End Using
                    End Using

                    ' Check if the author of the post is blocked by the user
                    Using reverseBlockCmd As New MySqlCommand(reverseBlockQuery, connection)
                        Using reader As MySqlDataReader = reverseBlockCmd.ExecuteReader()
                            If reader.Read() Then
                                ' Author of the post is blocked by the user, return an error
                                Return BadRequest("Something went wrong")
                            End If
                        End Using
                    End Using

                    Dim checkLikeStatusQuery As String = $"SELECT LikeStatus FROM likes WHERE post_id = {Convert.ToInt32(requestData("PostId"))} And user_id = {Convert.ToInt32(requestData("UserId"))} "

                    Using checkLikeStatusCmd As New MySqlCommand(checkLikeStatusQuery, connection)
                        Dim existingLikeStatus As String = Convert.ToString(checkLikeStatusCmd.ExecuteScalar())

                        If existingLikeStatus = "Liked" Then
                            Return BadRequest("Post already liked.")
                        ElseIf existingLikeStatus = "Unliked" Then
                            Dim updateLikesQuery As String = $"UPDATE likes Set LikeStatus = 'Liked' WHERE post_id = {Convert.ToInt32(requestData("PostId"))} AND user_id = {Convert.ToInt32(requestData("UserId"))} AND LikeStatus = 'Unliked';"
                            Using cmdUpdateLikes As New MySqlCommand(updateLikesQuery, connection)
                                cmdUpdateLikes.ExecuteNonQuery()
                            End Using
                            Dim updateNotifQuery As String = $"UPDATE notif SET notif_status = 'Active' WHERE reference_id = {Convert.ToInt32(requestData("PostId"))} AND sender_id = {Convert.ToInt32(requestData("UserId"))} AND reference_type = 'Liked' AND notif_status  = 'Inactive';"
                            Using cmdUpdateNotif As New MySqlCommand(updateNotifQuery, connection)
                                cmdUpdateNotif.ExecuteNonQuery()
                            End Using
                        ElseIf String.IsNullOrEmpty(existingLikeStatus) Then
                            Dim insertQueryLikes As String = $"INSERT INTO likes (post_id, user_id, LikeStatus, Like_Date) VALUES ({Convert.ToInt32(requestData("PostId"))}, {Convert.ToInt32(requestData("UserId"))}, 'Liked', NOW())"
                            Using cmdInsertLikes As New MySqlCommand(insertQueryLikes, connection)
                                cmdInsertLikes.ExecuteNonQuery()
                            End Using
                            Dim insertQueryNotif As String = $"INSERT INTO notif (notif_content, sender_id, receiver_id, notif_date, reference_id, reference_type, notif_status) VALUES ('Like your Post', {Convert.ToInt32(requestData("UserId"))}, (SELECT user_id FROM posts WHERE post_id = {Convert.ToInt32(requestData("PostId"))}), NOW(), {Convert.ToInt32(requestData("PostId"))}, 'Liked', 'Active')"
                            Using cmdInsertNotif As New MySqlCommand(insertQueryNotif, connection)
                                cmdInsertNotif.ExecuteNonQuery()
                            End Using
                        End If

                        Return Ok("Like data processed successfully.")
                    End Using
                End Using
            Catch ex As Exception
                Return InternalServerError(ex)
            End Try
        End Function





        <Route("api/Users/UnlikePost")>
        <HttpPost>
        Public Function UnlikePost() As IHttpActionResult
            Dim response As HttpResponseMessage = Request.CreateResponse(HttpStatusCode.Created)

            Try
                Dim requestBody As String = Request.Content.ReadAsStringAsync().Result
                Dim requestData As JObject = JsonConvert.DeserializeObject(Of JObject)(requestBody)

                Dim valid = True
                If IsNothing(requestData("PostId")) OrElse requestData("PostId").ToString().Trim() = "" Then
                    valid = False
                    response.StatusCode = HttpStatusCode.BadRequest
                    response.Content = New StringContent("Invalid request. PostId is required.")
                ElseIf IsNothing(requestData("UserId")) OrElse requestData("UserId").ToString().Trim() = "" Then
                    valid = False
                    response.StatusCode = HttpStatusCode.BadRequest
                    response.Content = New StringContent("Invalid request. UserId is required.")
                End If

                If Not valid Then
                    Return ResponseMessage(response)
                End If

                Using connection As New MySqlConnection(connectionString)
                    connection.Open()
                    Dim checkLikedQuery As String = $"SELECT COUNT(*) FROM likes WHERE post_id = {Convert.ToInt32(requestData("PostId"))} AND user_id = {Convert.ToInt32(requestData("UserId"))} AND LikeStatus = 'Unliked'"


                    Using checkLikedCmd As New MySqlCommand(checkLikedQuery, connection)
                        Dim likedCount As Integer = Convert.ToInt32(checkLikedCmd.ExecuteScalar())
                        If likedCount > 0 Then
                            Return BadRequest("Post already Unliked.")
                        End If
                    End Using



                    Dim updateQuery As String = $"UPDATE likes SET LikeStatus = 'Unliked' WHERE post_id = {Convert.ToInt32(requestData("PostId"))} AND user_id = {Convert.ToInt32(requestData("UserId"))} AND LikeStatus = 'Liked';" &
                            $"UPDATE notif SET notif_status = 'Inactive' WHERE reference_id = {Convert.ToInt32(requestData("PostId"))} AND sender_id = {Convert.ToInt32(requestData("UserId"))} AND reference_type = 'Liked' AND notif_status <> 'Inactive';"




                    Using cmdUpdate As New MySqlCommand(updateQuery, connection)
                            Dim rowsAffected As Integer = cmdUpdate.ExecuteNonQuery()
                            If rowsAffected = 0 Then
                                Return BadRequest("No existing Like record found to update.")
                            End If
                        End Using



                End Using

                Return Ok("UnLiked data processed successfully.")
            Catch ex As Exception
                Return InternalServerError(ex)
            End Try
        End Function

        <HttpPost>
        <Route("api/Users/GetFollowingPostLike")>
        Public Function GetFollowingPostLike()
            Dim functionModel As New FunctionModel()
            Try
                Dim requestBody As String = Request.Content.ReadAsStringAsync().Result
                Dim requestData = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(requestBody)

                If String.IsNullOrEmpty(requestData("UserId")) Then
                    Return BadRequest("User ID is required")
                End If

                Dim blockList As New List(Of Object)() ' Declaration moved here

                Using connection As New MySqlConnection(connectionString)
                    connection.Open()

                    ' Fetch block table data
                    Dim getBlockQuery As String = $"SELECT Blocker_id, Blocked_id, Block_status FROM block WHERE Blocker_id = '{requestData("UserId")}' OR Blocked_id = '{requestData("UserId")}' AND Block_status = 'Blocked';"
                    Using blockCmd As New MySqlCommand(getBlockQuery, connection)
                        Dim blockReader As MySqlDataReader = blockCmd.ExecuteReader()
                        While blockReader.Read()
                            Dim blockObject = New With {
                        .BlockerId = blockReader.GetString("Blocker_id"),
                        .BlockedId = blockReader.GetString("Blocked_id"),
                        .BlockStatus = blockReader.GetString("Block_status")
                    }
                            blockList.Add(blockObject)
                        End While
                        blockReader.Close()
                    End Using

                    ' Fetch posts related to followers
                    Dim getFollowersQuery As String = $"SELECT p.post_id, p.Caption, p.Location, p.Post_date, p.user_id, u.UserStatus FROM posts p " &
                                               $"JOIN users u ON p.user_id = u.user_id " &
                                               $"WHERE p.user_id IN (SELECT Followers_id FROM ff WHERE Followed_id = {requestData("UserId")} AND Status = 'Active') " &
                                               $"AND p.Status = 'Active';"

                    Using cmd As New MySqlCommand(getFollowersQuery, connection)
                        Dim reader As MySqlDataReader = cmd.ExecuteReader()
                        Dim postIdList As New List(Of Integer)()
                        Dim postList As New List(Of Object)()

                        While reader.Read()
                            Dim postId As Integer = reader.GetInt32("post_id")
                            postIdList.Add(postId)

                            Dim userStatus As String = reader.GetString("UserStatus")

                            If userStatus <> "Inactive" Then
                                Dim postObject = New With {
                            .UserId = reader.GetString("user_id"),
                            .PostId = postId,
                            .Caption = reader.GetString("Caption"),
                            .Location = reader.GetString("Location"),
                            .PostDate = reader.GetDateTime("Post_date"),
                            .LikeCount = 0,
                            .CommentCount = 0,
                            .Comments = New List(Of Object)(),
                            .LikeInfo = New List(Of Object)()
                        }
                                postList.Add(postObject)
                            End If
                        End While
                        reader.Close()

                        ' Fetch comment count and comments for each post
                        For Each postObject In postList
                            Dim getCommentCountQuery As String = $"SELECT COUNT(*) AS comment_count " &
                                                         $"FROM commenttbl " &
                                                         $"WHERE post_id = {postObject.PostId} AND status = 'Active';"

                            Using commentCountCmd As New MySqlCommand(getCommentCountQuery, connection)
                                postObject.CommentCount = Convert.ToInt32(commentCountCmd.ExecuteScalar())
                            End Using

                            Dim getCommentsQuery As String = $"SELECT c.post_id, c.user_id, u.username AS comment_username, u.UserStatus, c.comments, c.comment_date " &
                                                     $"FROM commenttbl c " &
                                                     $"JOIN users u ON c.user_id = u.user_id " &
                                                     $"WHERE c.post_id = {postObject.PostId} AND c.status = 'Active';"

                            Using commentsCmd As New MySqlCommand(getCommentsQuery, connection)
                                Dim commentsReader As MySqlDataReader = commentsCmd.ExecuteReader()
                                Dim commentsList As New List(Of Object)()

                                While commentsReader.Read()
                                    Dim commentUserId As String = commentsReader.GetString("user_id")
                                    Dim commentUsername As String = commentsReader.GetString("comment_username")
                                    Dim commentUserStatus As String = commentsReader.GetString("UserStatus")

                                    If commentUserStatus = "Inactive" Then
                                        commentUsername = "User"
                                    End If

                                    Dim cleanse As New FunctionModel()
                                    Dim commentObject = New With {
                                .PostId = commentsReader.GetInt32("post_id"),
                                .UserId = commentUserId,
                                .Username = commentUsername,
                                .Comments = cleanse.Uncleanse(commentsReader.GetString("comments")),
                                .CommentDate = commentsReader.GetDateTime("comment_date")
                            }
                                    commentsList.Add(commentObject)
                                End While

                                commentsReader.Close()
                                postObject.Comments = commentsList
                            End Using

                            ' Fetch like info for each post
                            Dim getLikeInfoQuery As String = $"SELECT l.user_id AS like_user_id, u.username AS like_username, u.UserStatus " &
                                                      $"FROM likes l " &
                                                      $"JOIN users u ON l.user_id = u.user_id " &
                                                      $"WHERE l.post_id = {postObject.PostId} AND l.LikeStatus = 'Liked';"

                            Using likeInfoCmd As New MySqlCommand(getLikeInfoQuery, connection)
                                Dim likeInfoReader As MySqlDataReader = likeInfoCmd.ExecuteReader()
                                Dim likeInfoList As New List(Of Object)()

                                While likeInfoReader.Read()
                                    Dim likeUserId As String = likeInfoReader.GetString("like_user_id")
                                    Dim likeUsername As String = likeInfoReader.GetString("like_username")
                                    Dim likeUserStatus As String = likeInfoReader.GetString("UserStatus")

                                    If likeUserStatus = "Inactive" Then
                                        likeUsername = "User"
                                    End If

                                    Dim likeInfoObject = New With {
                                .UserId = likeUserId,
                                .Username = likeUsername
                            }
                                    likeInfoList.Add(likeInfoObject)
                                End While

                                likeInfoReader.Close()
                                postObject.LikeInfo = likeInfoList
                                postObject.LikeCount = likeInfoList.Count
                            End Using
                        Next

                        ' Filter out posts from blocked users
                        Dim filteredPostList As New List(Of Object)()
                        For Each postObject In postList
                            Dim isBlocked As Boolean = False
                            For Each blockObject In blockList
                                If postObject.UserId = blockObject.BlockedId AndAlso requestData("UserId") = blockObject.BlockerId AndAlso blockObject.BlockStatus = "Blocked" Then
                                    isBlocked = True
                                ElseIf postObject.UserId = blockObject.BlockerId AndAlso requestData("UserId") = blockObject.BlockedId AndAlso blockObject.BlockStatus = "Blocked" Then
                                    isBlocked = True
                                    Exit For
                                End If
                            Next
                            If Not isBlocked Then
                                filteredPostList.Add(postObject)
                            End If
                        Next

                        ' Construct result object
                        Dim resultObject As New With {
                    .UserId = Integer.Parse(requestData("UserId")),
                    .PostIds = postIdList,
                    .Posts = filteredPostList,
                    .BlockList = blockList ' Add block list to result object
                }

                        Return Ok(resultObject)
                    End Using
                End Using
            Catch ex As Exception
                Return InternalServerError(ex)
            End Try
        End Function




        <HttpPost>
        <Route("api/Users/GetPostLikeId")>
        Public Function GetPostLikeId() As IHttpActionResult
            Dim response As HttpResponseMessage
            response = Request.CreateResponse(HttpStatusCode.Created)
            Try
                Dim requestBody As String = Request.Content.ReadAsStringAsync().Result
                Dim requestData = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(requestBody)

                Dim valid = True
                If IsNothing(requestData("PostId")) OrElse requestData("PostId").ToString().Trim() = "" Then
                    valid = False
                    response.StatusCode = HttpStatusCode.BadRequest
                    response.Content = New StringContent("Invalid request. user_id is required")
                End If

                If Not valid Then
                    Return ResponseMessage(response)
                End If

                ' Fetch block list for the post owner
                Dim blockList As New List(Of Object)()
                Using connection As New MySqlConnection(connectionString)
                    connection.Open()

                    Dim blockQuery As String = "SELECT Blocker_id, Blocked_id, Block_status FROM block;"
                    Using blockCmd As New MySqlCommand(blockQuery, connection)
                        Using blockReader As MySqlDataReader = blockCmd.ExecuteReader()
                            While blockReader.Read()
                                Dim blockObject As New With {
                            .BlockerId = blockReader.GetInt32("Blocker_id"),
                            .BlockedId = blockReader.GetInt32("Blocked_id"),
                            .BlockStatus = blockReader.GetString("Block_status")
                        }
                                blockList.Add(blockObject)
                            End While
                        End Using
                    End Using
                End Using

                ' Continue with the rest of the function...
                Using connection As New MySqlConnection(connectionString)
                    connection.Open()

                    Dim getPostQuery As String = $"SELECT p.post_id, p.user_id, p.Caption, p.Location, p.Post_date, u.UserStatus FROM posts p " &
                                         $"JOIN users u ON p.user_id = u.user_id " &
                                         $"WHERE p.post_id = {Convert.ToInt32(requestData("PostId"))} AND p.Status = 'Active';"

                    Using cmd As New MySqlCommand(getPostQuery, connection)
                        Using reader As MySqlDataReader = cmd.ExecuteReader()
                            If reader.HasRows Then
                                reader.Read()

                                Dim userStatus As String = reader.GetString("UserStatus")

                                If userStatus = "Inactive" Then
                                    Return StatusCode(HttpStatusCode.NotFound)
                                End If

                                Dim resultObject As New With {
                            .PostId = reader.GetInt32("post_id"),
                            .UserId = reader.GetInt32("user_id"),
                            .Caption = reader.GetString("Caption"),
                            .Location = reader.GetString("Location"),
                            .PostDate = reader.GetDateTime("Post_date"),
                            .CommentCount = 0,
                            .LikeCount = 0,
                            .Comments = Nothing,
                            .Likes = Nothing
                        }

                                Dim postUserId As Integer = resultObject.UserId

                                reader.Close()

                                Dim getCommentCountQuery As String = $"SELECT COUNT(*) AS comment_count FROM commenttbl WHERE post_id = {Convert.ToInt32(requestData("PostId"))} AND status = 'Active';"

                                Using commentCountCmd As New MySqlCommand(getCommentCountQuery, connection)
                                    resultObject.CommentCount = Convert.ToInt32(commentCountCmd.ExecuteScalar())
                                End Using

                                Dim getCommentsQuery As String = $"SELECT c.comment_id, c.post_id, c.user_id, u.username, u.UserStatus, c.comments, c.comment_date FROM commenttbl c " &
                                                         $"LEFT JOIN users u ON c.user_id = u.user_id " &
                                                         $"WHERE c.post_id = {Convert.ToInt32(requestData("PostId"))} AND c.status = 'Active' ORDER BY c.comment_date DESC;"

                                Using commentsCmd As New MySqlCommand(getCommentsQuery, connection)
                                    Using commentsReader As MySqlDataReader = commentsCmd.ExecuteReader()
                                        If commentsReader.HasRows Then
                                            Dim commentsList As New List(Of Object)()

                                            While commentsReader.Read()
                                                Dim commentUserId = commentsReader.GetInt32("user_id")
                                                Dim commentUsername = commentsReader.GetString("username")
                                                Dim commentUserStatus = commentsReader.GetString("UserStatus")

                                                If commentUserStatus = "Inactive" Then
                                                    commentUsername = "User"
                                                End If

                                                Dim commentObject As New With {
                                            .CommentId = commentsReader.GetInt32("comment_id"),
                                            .PostId = commentsReader.GetInt32("post_id"),
                                            .UserId = commentUserId,
                                            .Username = commentUsername,
                                            .Comments = commentsReader.GetString("comments"),
                                            .CommentDate = commentsReader.GetDateTime("comment_date")
                                        }

                                                commentsList.Add(commentObject)
                                            End While

                                            resultObject.Comments = commentsList
                                        End If
                                    End Using
                                End Using

                                Dim getLikesQuery As String = $"SELECT l.user_id AS like_user_id, u.username AS like_username, u.UserStatus " &
                                                      $"FROM likes l " &
                                                      $"JOIN users u ON l.user_id = u.user_id " &
                                                      $"WHERE l.post_id = {Convert.ToInt32(requestData("PostId"))} AND l.LikeStatus = 'Liked';"

                                Using likesCmd As New MySqlCommand(getLikesQuery, connection)
                                    Using likesReader As MySqlDataReader = likesCmd.ExecuteReader()
                                        If likesReader.HasRows Then
                                            Dim likesList As New List(Of Object)()

                                            While likesReader.Read()
                                                Dim likeUserId = likesReader.GetInt32("like_user_id")
                                                Dim likeUsername = likesReader.GetString("like_username")
                                                Dim likeUserStatus = likesReader.GetString("UserStatus")

                                                If likeUserStatus = "Inactive" Then
                                                    likeUsername = "User"
                                                End If

                                                Dim likeObject As New With {
                                            .UserId = likeUserId,
                                            .Username = likeUsername
                                        }

                                                likesList.Add(likeObject)
                                            End While

                                            resultObject.Likes = likesList
                                            resultObject.LikeCount = likesList.Count
                                        End If
                                    End Using
                                End Using

                                Return Ok(resultObject)
                            Else
                                Return NotFound()
                            End If
                        End Using
                    End Using
                End Using
            Catch ex As Exception
                Return InternalServerError(ex)
            End Try
        End Function



        <HttpPost>
        <Route("api/Users/GetNotification")>
        Public Function GetNotification() As IHttpActionResult
            Try
                Dim requestBody As String = Request.Content.ReadAsStringAsync().Result
                Dim requestData = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(requestBody)

                If String.IsNullOrEmpty(requestData("UserId")) Then
                    Return BadRequest("User ID is required")
                End If

                Using connection As New MySqlConnection(connectionString)
                    connection.Open()

                    Dim getNotificationQuery As String = $"SELECT n.notif_content, n.sender_id,n.reference_type, u.username AS sender_username, n.notif_date, n.reference_id, c.comments " &
                                     $"FROM notif n " &
                                     $"JOIN users u ON n.sender_id = u.user_id " &
                                     $"LEFT JOIN commenttbl c ON n.reference_id = c.comment_id " &
                                     $"WHERE n.receiver_id = {requestData("UserId")} AND n.notif_status = 'Active';"

                    Using cmd As New MySqlCommand(getNotificationQuery, connection)
                        Dim reader As MySqlDataReader = cmd.ExecuteReader()
                        Dim notificationList As New List(Of Object)()

                        Dim cleanse As New FunctionModel()

                        While reader.Read()
                            Dim notificationObject As Object
                            If reader.GetString("reference_type") = "Commented" Then
                                notificationObject = New With {
                                    .Content = If(Not reader.IsDBNull(reader.GetOrdinal("notif_content")), reader.GetString("notif_content"), Nothing),
                                    .SenderId = If(Not reader.IsDBNull(reader.GetOrdinal("sender_id")), reader.GetString("sender_id"), Nothing),
                                    .SenderUsername = If(Not reader.IsDBNull(reader.GetOrdinal("sender_username")), reader.GetString("sender_username"), Nothing),
                                    .Date = If(Not reader.IsDBNull(reader.GetOrdinal("notif_date")), reader.GetDateTime("notif_date"), Nothing),
                                    .CommentId = If(Not reader.IsDBNull(reader.GetOrdinal("reference_id")), reader.GetString("reference_id"), Nothing),
                                    .Comments = If(Not reader.IsDBNull(reader.GetOrdinal("Comments")), cleanse.Uncleanse(reader.GetString("Comments")), Nothing) ' Applying Uncleanse function to Comments
                                }
                            Else
                                notificationObject = New With {
                                    .Content = If(Not reader.IsDBNull(reader.GetOrdinal("notif_content")), reader.GetString("notif_content"), Nothing),
                                    .SenderId = If(Not reader.IsDBNull(reader.GetOrdinal("sender_id")), reader.GetString("sender_id"), Nothing),
                                    .SenderUsername = If(Not reader.IsDBNull(reader.GetOrdinal("sender_username")), reader.GetString("sender_username"), Nothing),
                                    .Date = If(Not reader.IsDBNull(reader.GetOrdinal("notif_date")), reader.GetDateTime("notif_date"), Nothing)
                                }
                            End If
                            notificationList.Add(notificationObject)
                        End While

                        reader.Close()

                        Dim resultObject As New With {
                            .UserId = Integer.Parse(requestData("UserId")),
                            .Notifications = notificationList
                        }

                        Return Ok(resultObject)
                    End Using
                End Using
            Catch ex As Exception
                Return InternalServerError(ex)
            End Try
        End Function

        <Route("api/Users/BlockPost")>
        <HttpPost>
        Public Function BlockPost()
            Dim functionModel As New FunctionModel()
            Try
                Dim requestBody As String = Request.Content.ReadAsStringAsync().Result
                Dim requestData As JObject = JsonConvert.DeserializeObject(Of JObject)(requestBody)

                If IsNothing(requestData("UserId")) OrElse requestData("UserId").ToString().Trim() = "" Then
                    Return BadRequest("UserId request data required.")
                ElseIf IsNothing(requestData("BlockedId")) OrElse requestData("BlockedId").ToString().Trim() = "" Then
                    Return BadRequest("BlockedId request data required.")
                End If

                Using connection As New MySqlConnection(connectionString)
                    connection.Open()

                    ' Check if the user to be blocked is in Deactivated Mode
                    Dim selectUserStatusQuery As String = $"SELECT UserStatus FROM users WHERE user_id = {Convert.ToInt32(requestData("BlockedId"))};"
                    Using selectUserStatusCmd As New MySqlCommand(selectUserStatusQuery, connection)
                        Using userStatusReader As MySqlDataReader = selectUserStatusCmd.ExecuteReader()
                            If userStatusReader.Read() Then
                                Dim userStatus As String = userStatusReader("UserStatus").ToString()
                                ' Check if the UserStatus is "Inactive"
                                If userStatus = "Inactive" Then
                                    ' If the UserStatus is "Inactive," return BadRequest with a message
                                    Return BadRequest("Cannot be blocked user is in Deactivated Mode")
                                End If
                            End If
                        End Using
                    End Using

                    Dim selectExistingBlockedQuery As String = $"SELECT Blocker_id, Blocked_id, Block_Status FROM Block WHERE Blocker_id = {Convert.ToInt32(requestData("BlockedId"))} AND Blocked_id = {Convert.ToInt32(requestData("UserId"))} AND Block_status = 'Blocked';"

                    Using selectExistingBlockedCmd As New MySqlCommand(selectExistingBlockedQuery, connection)
                        Using existingBlockedReader As MySqlDataReader = selectExistingBlockedCmd.ExecuteReader()
                            If existingBlockedReader.Read() Then
                                ' User is already blocked
                                Return BadRequest("User Not found or Block you")
                            End If
                        End Using
                    End Using

                    ' Check if the block already exists with status 'Blocked' by the user
                    Dim selectExistingBlockerQuery As String = $"SELECT Blocker_id, Blocked_id, Block_Status FROM Block WHERE Blocker_id = {Convert.ToInt32(requestData("UserId"))} AND Blocked_id = {Convert.ToInt32(requestData("BlockedId"))} AND Block_status = 'Blocked';"
                    Using selectExistingBlockerCmd As New MySqlCommand(selectExistingBlockerQuery, connection)
                        Using existingBlockerReader As MySqlDataReader = selectExistingBlockerCmd.ExecuteReader()
                            If existingBlockerReader.Read() Then
                                ' User has already blocked the requester
                                Return BadRequest("User is already blocked.")
                            End If
                        End Using
                    End Using

                    ' Check if the block exists with status 'Unblocked', if so, update to 'Blocked'
                    Dim updateQuery As String = $"UPDATE Block SET Block_status = 'Blocked' WHERE Blocker_id = {Convert.ToInt32(requestData("UserId"))} AND Blocked_id = {Convert.ToInt32(requestData("BlockedId"))} AND Block_status = 'Unblocked';"
                    Using updateCmd As New MySqlCommand(updateQuery, connection)
                        Dim rowsUpdated As Integer = updateCmd.ExecuteNonQuery()
                        If rowsUpdated > 0 Then
                            ' Block status updated from 'Unblocked' to 'Blocked'
                            ' Also, check if there's an active follow relation, if so, set it to 'Inactive'
                            Dim deactivateFollowBlockQuery As String = $"UPDATE ff SET Status = 'Inactive' WHERE Followed_id = {Convert.ToInt32(requestData("UserId"))} AND Followers_id = {Convert.ToInt32(requestData("BlockedId"))} AND Status = 'Active';"
                            Using deactivateFollowCmd As New MySqlCommand(deactivateFollowBlockQuery, connection)
                                deactivateFollowCmd.ExecuteNonQuery()
                            End Using
                            Return Ok("User blocked successfully.")
                        End If
                    End Using

                    ' Insert new block if no existing block found
                    Dim insertQuery As String = $"INSERT INTO Block (Blocker_id, Blocked_Id, Block_status) VALUES ({Convert.ToInt32(requestData("UserId"))}, {Convert.ToInt32(requestData("BlockedId"))}, 'Blocked');"
                    Using insertCmd As New MySqlCommand(insertQuery, connection)
                        insertCmd.ExecuteNonQuery()
                    End Using

                    ' Also, check if there's an active follow relation, if so, set it to 'Inactive'
                    Dim deactivateFollowInsertQuery As String = $"UPDATE ff SET Status = 'Inactive' WHERE Followed_id = {Convert.ToInt32(requestData("UserId"))} AND Followers_id = {Convert.ToInt32(requestData("BlockedId"))} AND Status = 'Active';"
                    Using deactivateFollowInsertCmd As New MySqlCommand(deactivateFollowInsertQuery, connection)
                        deactivateFollowInsertCmd.ExecuteNonQuery()
                    End Using

                    Return Ok("User blocked successfully.")
                End Using
            Catch ex As Exception
                Console.WriteLine(ex.ToString())
                Return InternalServerError(ex)
            End Try
        End Function



        <Route("api/Users/UnblockPost")>
        <HttpPost>
        Public Function UnblockPost()
            Try
                Dim requestBody As String = Request.Content.ReadAsStringAsync().Result
                Dim requestData As JObject = JsonConvert.DeserializeObject(Of JObject)(requestBody)

                If IsNothing(requestData("UserId")) OrElse requestData("UserId").ToString().Trim() = "" Then
                    Return BadRequest("UserId request data required.")
                ElseIf IsNothing(requestData("BlockedId")) OrElse requestData("BlockedId").ToString().Trim() = "" Then
                    Return BadRequest("BlockedId request data required.")
                End If

                Using connection As New MySqlConnection(connectionString)
                    connection.Open()

                    Dim selectQuery As String = $"SELECT Block_Status FROM Block WHERE Blocker_id = {Convert.ToInt32(requestData("UserId"))} AND Blocked_id = {Convert.ToInt32(requestData("BlockedId"))};"

                    Using selectCmd As New MySqlCommand(selectQuery, connection)
                        Dim blockStatus As Object = selectCmd.ExecuteScalar()

                        If blockStatus IsNot Nothing AndAlso blockStatus.ToString() = "Blocked" Then
                            Dim updateQuery As String = $"UPDATE Block SET Block_Status = 'Unblocked' WHERE Blocker_id = {Convert.ToInt32(requestData("UserId"))} AND Blocked_id = {Convert.ToInt32(requestData("BlockedId"))};"

                            Using updateCmd As New MySqlCommand(updateQuery, connection)
                                updateCmd.ExecuteNonQuery()
                            End Using

                            Return Ok("User unblocked successfully.")
                        Else
                            Return BadRequest("User already Unblock")
                        End If
                    End Using
                End Using
            Catch ex As Exception
                Console.WriteLine(ex.ToString())
                Return InternalServerError(ex)
            End Try
        End Function


        <Route("api/Users/GetBlocks")>
        <HttpPost>
        Public Function GetBlocks()
            Dim functionModel As New FunctionModel()
            Try
                Dim requestBody As String = Request.Content.ReadAsStringAsync().Result
                Dim requestData As JObject = JsonConvert.DeserializeObject(Of JObject)(requestBody)

                If IsNothing(requestData("UserId")) OrElse requestData("UserId").ToString().Trim() = "" Then
                    Return BadRequest("UserId request data required.")
                End If

                Using connection As New MySqlConnection(connectionString)
                    connection.Open()

                    Dim selectQuery As String = $"SELECT * FROM Block WHERE Blocker_id = {Convert.ToInt32(requestData("UserId"))} AND Block_status = 'Blocked';"

                    Using selectCmd As New MySqlCommand(selectQuery, connection)
                        Using reader As MySqlDataReader = selectCmd.ExecuteReader()
                            Dim blocks As New List(Of Object)()
                            While reader.Read()
                                Dim blockData As New Dictionary(Of String, Object)()
                                For i As Integer = 0 To reader.FieldCount - 1
                                    blockData.Add(reader.GetName(i), reader.GetValue(i))
                                Next
                                blocks.Add(blockData)
                            End While

                            ' Check if the list of blocks is empty
                            If blocks.Count = 0 Then
                                Return Ok("There are no block listed in your profile.")
                            Else
                                Return Ok(blocks)
                            End If
                        End Using
                    End Using
                End Using
            Catch ex As Exception
                Console.WriteLine(ex.ToString())
                Return InternalServerError(ex)
            End Try
        End Function


        <Route("api/Users/SearchUsers")>
        <HttpPost>
        Public Function SearchUsers()
            Dim functionModel As New FunctionModel()
            Try
                Dim requestBody As String = Request.Content.ReadAsStringAsync().Result
                Dim requestData As JObject = JsonConvert.DeserializeObject(Of JObject)(requestBody)

                Dim searcherIdOrEmailOrUsername As String = requestData("SearcherIdOrEmailOrUsername").ToString().Trim()
                Dim searchedIdOrEmailOrUsername As String = requestData("SearchedIdOrEmailOrUsername").ToString().Trim()

                If String.IsNullOrWhiteSpace(searcherIdOrEmailOrUsername) Then
                    Return BadRequest("SearcherIdOrEmailOrUsername is required")
                ElseIf String.IsNullOrWhiteSpace(searchedIdOrEmailOrUsername) Then
                    Return BadRequest("SearchedIdOrEmailOrUsername is required")
                End If

                Using connection As New MySqlConnection(connectionString)
                    connection.Open()

                    ' Find the searcher user ID by email or username
                    Dim searcherUserQuery As String = $"SELECT user_id FROM users WHERE email = @SearcherIdOrEmailOrUsername OR username = @SearcherIdOrEmailOrUsername;"
                    Using searcherUserCmd As New MySqlCommand(searcherUserQuery, connection)
                        searcherUserCmd.Parameters.AddWithValue("@SearcherIdOrEmailOrUsername", searcherIdOrEmailOrUsername)
                        Dim searcherUserId As Integer = Convert.ToInt32(searcherUserCmd.ExecuteScalar())
                        If searcherUserId = 0 Then
                            Return Ok("Searcher user not found")
                        End If

                        ' Check if the searched user is the same as the searcher
                        If searchedIdOrEmailOrUsername = searcherIdOrEmailOrUsername Then
                            Return Ok("You cannot search yourself")
                        End If

                        ' Find the searched user ID by email or username
                        Dim searchedUserQuery As String = $"SELECT user_id FROM users WHERE email = @SearchedIdOrEmailOrUsername OR username = @SearchedIdOrEmailOrUsername;"
                        Using searchedUserCmd As New MySqlCommand(searchedUserQuery, connection)
                            searchedUserCmd.Parameters.AddWithValue("@SearchedIdOrEmailOrUsername", searchedIdOrEmailOrUsername)
                            Dim searchedUserId As Integer = Convert.ToInt32(searchedUserCmd.ExecuteScalar())
                            If searchedUserId = 0 Then
                                Return Ok("Searched user not found")
                            End If

                            ' Check if the searcher has blocked the searched user
                            Dim blockCheckQuery As String = $"SELECT COUNT(*) FROM block WHERE (Blocker_id = {searcherUserId} AND Blocked_id = {searchedUserId}) AND Block_status = 'Blocked' OR (Blocker_id = {searchedUserId} AND Blocked_id = {searcherUserId} AND Block_status = 'Blocked' );"
                            Using blockCheckCmd As New MySqlCommand(blockCheckQuery, connection)
                                Dim blockCount As Integer = Convert.ToInt32(blockCheckCmd.ExecuteScalar())
                                If blockCount > 0 Then
                                    Return Ok("User cannot be searched")
                                End If
                            End Using

                            ' Check if the searched user is inactive
                            Dim userStatusQuery As String = $"SELECT UserStatus FROM users WHERE user_id = {searchedUserId};"
                            Using userStatusCmd As New MySqlCommand(userStatusQuery, connection)
                                Dim userStatus As String = Convert.ToString(userStatusCmd.ExecuteScalar())
                                If userStatus = "Inactive" Then
                                    Return Ok("User is in Deactivated mode")
                                End If
                            End Using

                            ' Retrieve searched user information
                            Dim selectQuery As String = $"SELECT user_id, username, email FROM users WHERE user_id = {searchedUserId};"
                            Using selectCmd As New MySqlCommand(selectQuery, connection)
                                Using reader As MySqlDataReader = selectCmd.ExecuteReader()
                                    If reader.Read() Then
                                        Dim userData As New Dictionary(Of String, Object)()
                                        userData.Add("UserId", reader("user_id"))
                                        userData.Add("username", reader("username"))
                                        userData.Add("email", reader("email"))
                                        Return Ok(userData)
                                    Else
                                        Return Ok("User not Found")
                                    End If
                                End Using
                            End Using
                        End Using
                    End Using
                End Using
            Catch ex As Exception
                Console.WriteLine(ex.ToString())
                Return InternalServerError(ex)
            End Try
        End Function





        <Route("api/Chats/Insert")>
        Public Function InsertChat()
            Dim functionModel As New FunctionModel()

            Try
                Dim requestBody As String = Request.Content.ReadAsStringAsync().Result
                Dim requestData As JObject = JsonConvert.DeserializeObject(Of JObject)(requestBody)

                If IsNothing(requestData("SenderId")) OrElse requestData("SenderId").ToString().Trim() = "" Then
                    Return BadRequest("SenderId request data required.")
                ElseIf IsNothing(requestData("ReceiverId")) OrElse requestData("ReceiverId").ToString().Trim() = "" Then
                    Return BadRequest("ReceiverId request data required.")
                ElseIf IsNothing(requestData("Message")) OrElse requestData("Message").ToString().Trim() = "" Then
                    Return BadRequest("Message request data required.")
                End If

                ' Cleanse the message before insertion
                Dim cleansedMessage As String = functionModel.Cleanse(requestData("Message").ToString())

                ' Check if Sender or Receiver is blocked
                Dim senderId As Integer = Convert.ToInt32(requestData("SenderId"))
                Dim receiverId As Integer = Convert.ToInt32(requestData("ReceiverId"))

                If UserBlocked(senderId, receiverId) Then
                    Return BadRequest("Cannot send message. Sender or Receiver is blocked.")
                End If

                ' Check if ReceiverId exists in the users table
                If Not UserExists(receiverId) Then
                    Return BadRequest("User not found in the users table.")
                End If

                ' Check if Receiver is inactive
                If Not IsUserActive(receiverId) Then
                    Return BadRequest("User is deactivated.")
                End If

                Using connection As New MySqlConnection(connectionString)
                    connection.Open()

                    Dim insertChatQuery As String = $"INSERT INTO Chat (Chatsender_id, Chatreceiver_id, Chats, Sent_date, Read_date, Message_status) VALUES ({senderId}, {receiverId}, '{cleansedMessage}', NOW(), NULL, 'Unread');"

                    Using insertChatCmd As New MySqlCommand(insertChatQuery, connection)
                        insertChatCmd.ExecuteNonQuery()
                    End Using

                    Return Ok("Message sent successfully.")
                End Using
            Catch ex As Exception
                Console.WriteLine(ex.ToString())
                Return InternalServerError(ex)
            End Try
        End Function

        Private Function UserBlocked(blockerId As Integer, blockedId As Integer) As Boolean
            ' Check if the user is blocked
            Using connection As New MySqlConnection(connectionString)
                connection.Open()

                Dim query As String = $"SELECT * FROM block WHERE (blocker_id = {blockerId} AND blocked_id = {blockedId} AND block_status = 'Blocked') OR (blocker_id = {blockedId} AND blocked_id = {blockerId} AND block_status = 'Blocked');"

                Using command As New MySqlCommand(query, connection)
                    Using reader As MySqlDataReader = command.ExecuteReader()
                        ' Check if any rows are returned
                        Return reader.HasRows
                    End Using
                End Using
            End Using
        End Function

        Public Function UserExists(userId As Integer) As Boolean
            ' Check if the user exists in the users table
            Using connection As New MySqlConnection(connectionString)
                connection.Open()

                Dim query As String = $"SELECT username,email FROM users WHERE user_id = {userId};"
                Using command As New MySqlCommand(query, connection)
                    Using reader As MySqlDataReader = command.ExecuteReader()
                        ' Check if any rows are returned
                        Return reader.HasRows
                    End Using
                End Using
            End Using
        End Function

        Private Function IsUserActive(userId As Integer) As Boolean
            ' Check if the user is active
            Using connection As New MySqlConnection(connectionString)
                connection.Open()

                Dim query As String = $"SELECT UserStatus FROM users WHERE user_id = {userId};"

                Using command As New MySqlCommand(query, connection)
                    Dim userStatus As String = command.ExecuteScalar()?.ToString()

                    ' Check if user status is active
                    Return userStatus = "Active"
                End Using
            End Using
        End Function





        <Route("api/Chats/Select")>
        <HttpPost>
        Public Function SelectChat() As IHttpActionResult
            Dim functionModel As New FunctionModel()

            Try
                Dim requestBody As String = Request.Content.ReadAsStringAsync().Result
                Dim requestData As JObject = JsonConvert.DeserializeObject(Of JObject)(requestBody)

                ' Check if ReceiverId exists in the users table
                Using connection As New MySqlConnection(connectionString)
                    connection.Open()

                    Dim userCheckQuery As String = $"SELECT COUNT(*) FROM users WHERE user_id = {Convert.ToInt32(requestData("ReceiverId"))};"

                    Using userCheckCmd As New MySqlCommand(userCheckQuery, connection)
                        Dim userExists As Integer = Convert.ToInt32(userCheckCmd.ExecuteScalar())

                        If userExists = 0 Then
                            ' If ReceiverId doesn't exist, return "User not found" message
                            Return BadRequest("User not found")
                        End If
                    End Using
                End Using

                ' If ReceiverId exists, proceed to fetch chat data
                Using connection As New MySqlConnection(connectionString)
                    connection.Open()

                    Dim updateQuery As String = $"
                UPDATE chat 
                SET Read_date = NOW(), Message_status = 'Read' 
                WHERE Chatsender_id = {Convert.ToInt32(requestData("ReceiverId"))} 
                AND Chatreceiver_id = {Convert.ToInt32(requestData("SenderId"))};"

                    Using updateCmd As New MySqlCommand(updateQuery, connection)
                        updateCmd.ExecuteNonQuery()
                    End Using

                    Dim selectChatQuery As String = $"
                SELECT
                    chat.Chat_id,
                    sender.user_id AS sender_id,
                    sender.username AS sender_username,
                    receiver.user_id AS receiver_id,
                    receiver.username AS receiver_username,
                    chat.Chats,
                    chat.Sent_date,
                    chat.Message_status
                FROM
                    chat
                JOIN
                    users AS sender ON chat.Chatsender_id = sender.user_id
                JOIN
                    users AS receiver ON chat.Chatreceiver_id = receiver.user_id
                WHERE
                    (chat.Chatsender_id = {Convert.ToInt32(requestData("SenderId"))} AND chat.Chatreceiver_id = {Convert.ToInt32(requestData("ReceiverId"))})
                    OR
                    (chat.Chatsender_id = {Convert.ToInt32(requestData("ReceiverId"))} AND chat.Chatreceiver_id = {Convert.ToInt32(requestData("SenderId"))})
                ;"

                    Using selectChatCmd As New MySqlCommand(selectChatQuery, connection)
                        Using reader As MySqlDataReader = selectChatCmd.ExecuteReader()
                            Dim chatList As New List(Of Object)()

                            While reader.Read()
                                Dim chatData As New With {
                            .Chat_id = reader("Chat_id"),
                            .sender_id = reader("sender_id"),
                            .sender_username = reader("sender_username"),
                            .Chats = functionModel.Uncleanse(reader("Chats")),
                            .receiver_id = reader("receiver_id"),
                            .receiver_username = reader("receiver_username"),
                            .Sent_date = reader("Sent_date"),
                            .Message_status = reader("Message_status")
                        }

                                chatList.Add(chatData)
                            End While

                            Return Ok(chatList)
                        End Using
                    End Using
                End Using
            Catch ex As Exception
                Console.WriteLine(ex.ToString())
                Return InternalServerError(ex)
            End Try
        End Function



        <Route("api/Users/DeactivateUser")>
        <HttpPost>
        Public Function DeactivateUser() As IHttpActionResult
            Dim response As HttpResponseMessage = Request.CreateResponse(HttpStatusCode.Created)

            Try
                Dim requestBody As String = Request.Content.ReadAsStringAsync().Result
                Dim requestData As JObject = JsonConvert.DeserializeObject(Of JObject)(requestBody)

                Dim valid = True
                If IsNothing(requestData("UserId")) OrElse requestData("UserId").ToString().Trim() = "" Then
                    valid = False
                    response.StatusCode = HttpStatusCode.BadRequest
                    response.Content = New StringContent("Invalid request. UserId is required.")
                End If

                If Not valid Then
                    Return ResponseMessage(response)
                End If

                Using connection As New MySqlConnection(connectionString)
                    connection.Open()

                    Dim updateQuery As String = $"UPDATE users SET UserStatus = 'Inactive' WHERE user_id = {Convert.ToInt32(requestData("UserId"))};"

                    Using cmdUpdate As New MySqlCommand(updateQuery, connection)
                        Dim rowsAffected As Integer = cmdUpdate.ExecuteNonQuery()
                        If rowsAffected = 0 Then
                            Return BadRequest("No user found to deactivate.")
                        End If
                    End Using
                End Using

                Return Ok("User deactivated successfully.")
            Catch ex As Exception
                Return InternalServerError(ex)
            End Try
        End Function

        <Route("api/Users/ActivateUser")>
        <HttpPost>
        Public Function ActivateUser() As IHttpActionResult
            Dim response As HttpResponseMessage = Request.CreateResponse(HttpStatusCode.Created)

            Try
                Dim requestBody As String = Request.Content.ReadAsStringAsync().Result
                Dim requestData As JObject = JsonConvert.DeserializeObject(Of JObject)(requestBody)

                Dim valid = True
                If IsNothing(requestData("UserId")) OrElse requestData("UserId").ToString().Trim() = "" Then
                    valid = False
                    response.StatusCode = HttpStatusCode.BadRequest
                    response.Content = New StringContent("Invalid request. UserId is required.")
                End If

                If Not valid Then
                    Return ResponseMessage(response)
                End If

                Using connection As New MySqlConnection(connectionString)
                    connection.Open()

                    Dim updateQuery As String = $"UPDATE users SET UserStatus = 'Active' WHERE user_id = {Convert.ToInt32(requestData("UserId"))};"

                    Using cmdUpdate As New MySqlCommand(updateQuery, connection)
                        Dim rowsAffected As Integer = cmdUpdate.ExecuteNonQuery()
                        If rowsAffected = 0 Then
                            Return BadRequest("No user found to activate.")
                        End If
                    End Using
                End Using

                Return Ok("User activated successfully.")
            Catch ex As Exception
                Return InternalServerError(ex)
            End Try
        End Function



    End Class
End Namespace
