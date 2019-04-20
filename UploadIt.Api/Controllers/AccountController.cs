﻿using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using UploadIt.Data.Db.Account;
using UploadIt.Data.Models.Account;
using UploadIt.Services.Account;
using UploadIt.Services.Security;

namespace UploadIt.Api.Controllers
{
    /// <summary>
    /// Controller used to issue jwt tokens and manage user accounts
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IConfiguration _config;
        private readonly ITokenGenerator _tokenGenerator;

        public AccountController(IUserService userService,
                                 IConfiguration config,
                                 ITokenGenerator tokenGenerator)
        {
            _userService = userService;
            _config = config;
            this._tokenGenerator = tokenGenerator;
        }

        /// <summary>
        /// Authenticates the user based on the data provided in <paramref name="form"/> and returns user info and a jwt token if authentication succeeds
        /// </summary>
        /// <param name="form"></param>
        /// <returns>Object containing: string userName, string email, string token, DateTime validTo</returns>
        [AllowAnonymous]
        [Route("Authenticate")]
        [HttpPost]
        public IActionResult Authenticate([FromForm]LoginForm form)
        {
            User user = null;
            try
            {
                user = _userService.Authenticate(form.UserName, form.Password);
            }
            catch (ArgumentException e)
            {
                Console.WriteLine(e);
            }
            if (user == null)
            {
                return BadRequest("Invalid credentials");
            }

            Claim[] claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Id.ToString())
            };


            var tokenInfo = _tokenGenerator.GenerateJwtToken(_config.GetValue<string>("AppSettings:Secret"), claims, 15);

            return Ok(new
            {
                userName = user.UserName,
                email = user.Email,
                token = tokenInfo.Token,
                validTo = tokenInfo.ValidTo
            });
        }

        /// <summary>
        /// Registers a user using the data provided in <paramref name="form"/>
        /// </summary>
        /// <param name="form"></param>
        /// <returns>String which describes if the operation succeeded</returns>
        [AllowAnonymous]
        [Route("Register")]
        [HttpPost]
        public async Task<IActionResult> Register([FromForm]RegisterForm form)
        {
            User user;
            try
            {
                user = await _userService.AddUserAsync(form.UserName,
                   form.Password,
                   form.Email);
            }
            catch (ArgumentException e)
            {
                Console.WriteLine(e);
                return BadRequest("Invalid form data");
            }

            if (user == null)
            {
                return BadRequest(
                    "User with the provided username or email already exists");
            }

            return Ok("Account created");
        }

        /// <summary>
        /// Deletes the user bound to the jwtToken which was used for authorization
        /// </summary>
        /// <returns>String which describes if the operation succeeded</returns>
        [Route("Delete")]
        [HttpDelete]
        public async Task<IActionResult> Delete()
        {
            var userIdString = User.Identity.Name;

            if (!int.TryParse(userIdString, out int userId))
            {
                return BadRequest("Invalid user id");
            }

            try
            {
                await _userService.DeleteUser(userId);
            }
            catch (ArgumentException e)
            {
                Console.WriteLine(e);
                return BadRequest(e.Message);
            }
            return Ok($"User with id {userId} successfully deleted");
        }

        /// <summary>
        /// Returns the user data bound to the jwtToken which was used for authorization
        /// </summary>
        /// <returns>If succeeded returns an object containing: int userId, string userName, string email</returns>
        [Route("Get")]
        [HttpGet]
        public async Task<IActionResult> GetById()
        {
            var userIdString = User.Identity.Name;

            if (!int.TryParse(userIdString, out int userId))
            {
                return BadRequest("Invalid user id");
            }

            var user = await _userService.GetUserByIdAsync(userId);

            return Ok(new
            {
                user.Id,
                user.UserName,
                user.Email
            });
        }
    }
}