using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Product.API.Infrastuctures.Repositiories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Product.API.Controllers.v1
{

    [ApiController]
    //[ApiVersion("1.0")]
    [Route("api")]
    [Produces("application/json")]
    public class AccountController : ControllerBase
    {
        private readonly IAccountRepository _accountRepository;

        public AccountController(IAccountRepository accountRepository)
        {
            _accountRepository = accountRepository;
        }

        [HttpGet("account/dapper")]
        public async Task<IActionResult> GetDapper()
        {
            try
            {
                var data = await _accountRepository.GetAllAsync();
                return Ok(data);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpGet("account/sqlkata")]
        public async Task<IActionResult> GetSqlKata()
        {
            try
            {
                var data = await _accountRepository.GetAllSqlKataAsync();
                return Ok(data);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
