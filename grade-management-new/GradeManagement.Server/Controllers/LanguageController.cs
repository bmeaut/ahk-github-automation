using GradeManagement.Bll;
using GradeManagement.Bll.Services;
using GradeManagement.Server.Controllers.BaseControllers;
using GradeManagement.Shared.Dtos;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GradeManagement.Server.Controllers;

[Route("api/languages")]
[ApiController]
public class LanguageController(LanguageService languageService)
    : RestrictedCrudControllerBase<Language>(languageService);
