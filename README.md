# UserManagementAPI
The code created in this repository is meant for peer review assessment as part of the Coursera course "Backend Development with DotNet".

To test this code use a client like Postman.

1. First, type 'dotnet run'
2. For all the steps below also check that appropriate logging is returned on the CLI.
3. Go to various endpoints and check that authentication fails based on lack of a valid token.
   GET http://localhost:<YOUR PORT>/api/users/1
   POST http://localhost:<YOUR PORT>/api/users/
   PUT http://localhost:<YOUR PORT>/api/users/
   DELETE http://localhost:<YOUR PORT>/api/users/

4. Now athenticate by retrieving a JWT token from the following endpoint
5. POST http://localhost:<YOUR PORT>/api/auth/token
    body {
    "Username" : "JoeBloggs",
    "Password" : "passw0rd"
}
Paste the token into the authorisation header (bearer option in Postman).
6. Check the following endpoint is authorised but returns an error for unknown user
   GET http://localhost:5231/api/users/150
7. Add a user using with the following command.
   POST http://localhost:<YOUR PORT>/api/users/
   body {
    "firstName": "Joe",
    "lastName": "Bloggs",
    "email": "joe@bloggs.com",
    "phoneNumber": "0777 777777"
}
check that a 201 status code is returned.
8. Attempt to retrieve the new user using GET
   GET http://localhost:<YOUR PORT>/api/users/1 
9. Add another new user using step (7) with new details
10. Attempt to retrieve both added users.
GET http://localhost:<YOUR PORT>/api/users/
Check that both users are returned.
11. Change User 1 using a PUT request
PUT http://localhost:<YOUR PORT>/api/users/1
   body {
    "firstName": "Joe",
    "lastName": "Smith",
    "email": "joe@bloggs.com",
    "phoneNumber": "0777 777777"
}
12. Repeat step (10) to check the user has changed.
13. DELETE user 1 using a DELETE request
    DELETE http://localhost:<YOUR PORT>/api/users/1
14. Repeat step (10) to check the user has been deleted (only user with id 2 is returned)
15. Attempt to add a new user using step 6 but with a first name missing.
    body {
      "firstName": "",
      "lastName": "Smith",
      "email": "joe@bloggs.com",
      "phoneNumber": "0777 777777"
    }
    You should see a JSON respond stating the object validation has failed and the reason why.
16. Go to a disallowed API route
    POST http://localhost:<YOUR PORT>/api/users/150 
    and check you receive a JSON rsponse detailing that the route is invalid and cannot be accessed (status code 405).
