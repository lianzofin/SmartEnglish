using DAL;
using Microsoft.AspNetCore.Mvc;
using Model;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly UserDB userDB;

        public UsersController(UserDB userDB)
        {
            this.userDB = userDB;
        }

        // שליפת כל המשתמשים (מאפס סיסמאות לאבטחה)
        [HttpGet]
        public async Task<ActionResult<List<User>>> GetAll()
        {
            var users = await userDB.GetAllUsersAsync();
            foreach (var user in users)
            {
                user.Password = string.Empty;
            }
            return Ok(users);
        }

        // שליפת משתמש לפי מזהה
        [HttpGet("{id:int}")]
        public async Task<ActionResult<User>> GetById(int id)
        {
            User? user = await userDB.GetUserByIdAsync(id);
            if (user == null)
                return NotFound();

            user.Password = string.Empty;
            return Ok(user);
        }

        // בדיקת זמינות שם משתמש
        [HttpGet("exists/{username}")]
        public async Task<ActionResult<bool>> UsernameExists(string username)
        {
            return Ok(await userDB.UsernameExistsAsync(username));
        }

        // בדיקת זמינות אימייל
        [HttpGet("email-exists/{email}")]
        public async Task<ActionResult<bool>> EmailExists(string email)
        {
            return Ok(await userDB.EmailExistsAsync(email));
        }

        // קבלת כמות משתמשים לפי תפקידים
        [HttpGet("count-by-role")]
        public async Task<ActionResult<Dictionary<string, int>>> CountByRole()
        {
            return Ok(await userDB.CountUsersByRoleAsync());
        }

        // הרשמת משתמש חדש
        [HttpPost]
        public async Task<ActionResult<User>> Add([FromBody] User user, [FromQuery] string roleName = "Student")
        {
            if (await userDB.UsernameExistsAsync(user.Username))
                return Conflict("Username already exists.");

            if (await userDB.EmailExistsAsync(user.Email))
                return Conflict("Email already exists.");

            try
            {
                await userDB.AddUserAsync(user, roleName);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }

            user.Password = string.Empty;
            return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
        }

        // אימות פרטי התחברות
        [HttpPost("login")]
        public async Task<ActionResult<User>> Login([FromBody] User credentials)
        {
            User? user = await userDB.CheckLoginAsync(credentials.Username, credentials.Password);
            if (user == null)
                return Unauthorized();

            user.Password = string.Empty;
            return Ok(user);
        }

        // עדכון תפקיד משתמש
        [HttpPut("{id:int}/role")]
        public async Task<IActionResult> UpdateRole(int id, [FromQuery] string roleName)
        {
            return await userDB.UpdateUserRoleAsync(id, roleName) ? NoContent() : NotFound();
        }

        // עדכון סיסמת משתמש
        [HttpPut("{id:int}/password")]
        public async Task<IActionResult> UpdatePassword(int id, [FromBody] string newPassword)
        {
            return await userDB.UpdatePasswordAsync(id, newPassword) ? NoContent() : NotFound();
        }

        // עדכון כתובת אימייל
        [HttpPut("{id:int}/email")]
        public async Task<IActionResult> UpdateEmail(int id, [FromBody] string newEmail)
        {
            if (await userDB.EmailExistsAsync(newEmail))
                return Conflict("Email already exists.");

            return await userDB.UpdateEmailAsync(id, newEmail) ? NoContent() : NotFound();
        }

        // מחיקת משתמש
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            return await userDB.DeleteUserAsync(id) ? NoContent() : NotFound();
        }
    }
}