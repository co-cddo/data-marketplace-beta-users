using cddo_users.DTOs;
using cddo_users.Interface;
using cddo_users.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace cddo_users.Api
{
    [Route("/Department")]
    [ApiController]
    [Authorize] // Ensure that the user is authenticated
    public class DepartmentController : ControllerBase
    {
        private readonly IDepartmentService _departmentService;
        private readonly IUserService _userService;

        public DepartmentController(IDepartmentService departmentService, IUserService userService)
        {
            _departmentService = departmentService;
            _userService = userService;
        }

        [HttpPost("Assign/{departmentId}/{organisationId}")]
        public async Task<IActionResult> AssignOrganisationToDepartment(int departmentId, int organisationId)
        {
            try
            {
                return Ok(await _departmentService.AssignOrganisationToDepartmentAsync(departmentId, organisationId));
            }
            catch (Exception)
            {

                return Ok(false);
            }

        }

        [HttpPost("re-assign/{departmentId}/{organisationId}")]
        public async Task<IActionResult> ReAssignOrganisationToDepartment(int departmentId, int organisationId)
        {
            try
            {
                return Ok(await _departmentService.ReAssignOrganisationToDepartmentAsync(departmentId, organisationId));
            }
            catch (Exception)
            {

                return Ok(false);
            }
        }

        [HttpPost("un-assign/{departmentId}/{organisationId}")]
        public async Task<IActionResult> UnAssignOrganisationToDepartment(int departmentId, int organisationId)
        {
            try
            {
                return Ok(await _departmentService.UnAssignOrganisationToDepartmentAsync(departmentId, organisationId));
            }
            catch (Exception)
            {

                return Ok(false);
            }
        }

        [HttpGet("un-assigned-organisations")]
        public async Task<IActionResult> GetUnAssignedOrganisation()
        {
            try
            {
                return Ok(await _departmentService.GetUnAssignedOrganisation());
            }
            catch (Exception)
            {

                return Ok(null);
            }
        }

        [HttpGet("assigned-organisations")]
        public async Task<IActionResult> GetAllAssignedOrganisation()
        {
            try
            {
                return Ok(await _departmentService.GetAllAssignedOrganisation());
            }
            catch (Exception)
            {

                return Ok(null);
            }
        }
        [HttpGet("assigned-organisations/{departmentId}")]
        public async Task<IActionResult> GetAssignedOrganisations(int departmentId)
        {
            try
            {
                return Ok(await _departmentService.GetAssignedOrganisations(departmentId));
            }
            catch (Exception)
            {

                return Ok(null);
            }
        }

        [HttpGet("department/{departmentId}")]
        public async Task<IActionResult> GetDepartmentById(int departmentId)
        {
            try
            {
                return Ok(await _departmentService.GetDepartmentByIdAsync(departmentId));
            }
            catch (Exception)
            {

                return Ok(null);
            }
        }

        [HttpGet("departments-paged")]
        public async Task<IActionResult> GetAllPagedDepartments(int page, int pageSize, string? searchTerm = null)
        {
            try
            {
                if (page <= 0)
                {
                    page = 1;
                }

                if (pageSize <= 0)
                {
                    pageSize = 10;
                }

                var result = await _departmentService.GetAllPagedDepartmentsAsync(page, pageSize, searchTerm);
                var response = new PaginatedDepartments()
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    Departments = result.Item2.ToList(),
                    TotalCount = result.Item1,
                };

                return Ok(response);
            }
            catch (Exception)
            {

                return Ok(null);
            }
        }

        [HttpGet("departments")]
        public async Task<IActionResult> GetAllDepartments()
        {
            try
            {
                var result = await _departmentService.GetAllDepartmentsAsync();

                return Ok(result.ToList());
            }
            catch (Exception)
            {

                return Ok(null);
            }
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateDepartment([FromBody] string departmentName)
        {
            try
            {
                var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email || c.Type == "email")?.Value;
                if (userEmail == null) return BadRequest("User Email could not be found");

                var user = await _userService.GetUserByEmailAsync(userEmail);
                if (user == null) return BadRequest("User could not be found");

                var result = await _departmentService.CreateDepartmentAsync(departmentName, user.User.UserId);

                if (result != null)
                {
                    return Ok(result);
                }
                else
                {
                    return BadRequest("Error creating department.");
                }
            }
            catch (System.Exception ex)
            {
                // Log the exception
                return StatusCode(500, new { message = "An error occurred while processing your request.", details = ex.Message });
            }
        }
    }
}
