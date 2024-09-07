using GradeManagement.Bll;
using GradeManagement.Bll.Services;
using GradeManagement.Server.Controllers.BaseControllers;
using GradeManagement.Shared.Dtos;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GradeManagement.Server.Controllers;

[Authorize]
[Route("api/semesters")]
[ApiController]
public class SemesterController(SemesterService semesterService) : RestrictedCrudControllerBase<Semester>(semesterService);
