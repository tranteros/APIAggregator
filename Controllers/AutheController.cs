using APIAggregator.Services;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;

namespace APIAggregator.Controllers {

    [Route("api/auth")]
    public class AutheController : Controller {

        private readonly AuthService _authService;

        public AutheController(AuthService authService) {
            _authService = authService;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request) 
        {

            if (request.Email == "test" && request.Password == "password")
            {
                var token = _authService.GenerateJwtToken(request.Email);
                return Ok(new { Token = token });
            }

            return Unauthorized();
        }
    }
}
