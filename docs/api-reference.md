# Endpoint inventory

Both columns use the prefix `/api`. Each method in a row applies to both routes.

| Method | Spring route | ASP.NET Core route |
|---|---|---|
| GET, POST | /task | /tasks |
| GET, DELETE | /task/{id} | /tasks/{id} |
| PUT | /task/updateState/{id} | /tasks/{id}/state |
| PUT | /task/updateDescription/{id} | /tasks/{id}/description |
| GET, POST | /user | /users |
| GET, PUT, DELETE | /user/{id} | /users/{id} |
| POST, GET, DELETE | /task/{id}/comments | /tasks/{id}/comments |
| GET, DELETE | /comments/{id} | /comments/{id} |
| GET, DELETE | /user/{id}/comments | /users/{id}/comments |
| GET | /history/{id} | /tasks/{id}/history |
| POST, DELETE | /usertask/{userId}/{taskId} | /tasks/{taskId}/assignees/{userId} |
| GET | /getAllTasksOfUser/{id} | /users/{id}/assignments |
| GET | /getAllUsersOfTask/{id} | /tasks/{id}/assignments |
| GET | /getUserTaskDetailsWithDescription | /assignments |
| GET | /getCommentsWithUserIdAndTaskId | /assignments/comments |

Java accepts entity-shaped task/user writes and a CommentSchema containing userId and comment. Several reads return entities with hidden foreign keys or positional SQL rows. The alternative uses named response records. User search supports `?name=`. All lists use ordered offset pagination (`offset=0&limit=50`, maximum 100) and return 200 with an array. Creates return 201, deletes 204, updates 200. Validation returns 400, absent resources 404, duplicate assignments and concurrent write conflicts 409.

The assignment-comment report matches **both** foreign keys and returns individual comment records. The Java native query joins a third table on the assignment and comment task IDs and selects DISTINCT text, which can collapse identical comments. Bulk comment deletion produces no history, consistent with Java. Individual comment changes produce history. Assignment history records the user ID. Task history uses its parent task ID. Comment history records the comment ID even after deletion.
