﻿using AutoMapper;
using AutoMapper.QueryableExtensions;

using AutSoft.Common.Exceptions;
using AutSoft.Linq.Queryable;

using GradeManagement.Bll.BaseServices;
using GradeManagement.Data.Data;
using GradeManagement.Shared.Dtos;
using GradeManagement.Shared.Dtos.Response;

using Microsoft.EntityFrameworkCore;

namespace GradeManagement.Bll;

public class UserService : ICrudServiceBase<User>
{
    private readonly GradeManagementDbContext _gradeManagementDbContext;
    private readonly IMapper _mapper;

    public UserService(GradeManagementDbContext gradeManagementDbContext, IMapper mapper)
    {
        _gradeManagementDbContext = gradeManagementDbContext;
        _mapper = mapper;
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        return await _gradeManagementDbContext.User
            .ProjectTo<User>(_mapper.ConfigurationProvider)
            .ToListAsync();
    }

    public async Task<User> GetByIdAsync(long id)
    {
        return await _gradeManagementDbContext.User
            .ProjectTo<User>(_mapper.ConfigurationProvider)
            .SingleEntityAsync(u => u.Id == id, id);
    }

    public async Task<User> CreateAsync(User requestDto)
    {
        var userEntity = new Data.Models.User
        {
            Name = requestDto.Name,
            NeptunCode = requestDto.NeptunCode,
            GithubId = requestDto.GithubId,
            BmeEmail = requestDto.BmeEmail,
            Type = requestDto.Type
        };
        _gradeManagementDbContext.User.Add(userEntity);
        await _gradeManagementDbContext.SaveChangesAsync();
        return _mapper.Map<User>(userEntity);
    }

    public async Task DeleteAsync(long id)
    {
        var userEntity = await _gradeManagementDbContext.User.SingleEntityAsync(u => u.Id == id, id);
        _gradeManagementDbContext.User.Remove(userEntity);
        await _gradeManagementDbContext.SaveChangesAsync();
    }

    public async Task<User> UpdateAsync(long id, User requestDto)
    {
        if (requestDto.Id != id)
        {
            throw new ValidationException("ID", id.ToString(),
                "The Id from the query and the Id of the DTO do not match!");
        }

        var userEntity = await _gradeManagementDbContext.User.SingleEntityAsync(u => u.Id == id, id);
        userEntity.Name = requestDto.Name;
        userEntity.NeptunCode = requestDto.NeptunCode;
        userEntity.GithubId = requestDto.GithubId;
        userEntity.BmeEmail = requestDto.BmeEmail;
        userEntity.Type = requestDto.Type;

        await _gradeManagementDbContext.SaveChangesAsync();
        return _mapper.Map<User>(userEntity);
    }

    public async Task<List<Group>> GetAllGroupsByIdAsync(long id)
    {
        return await _gradeManagementDbContext.User
            .Where(u => u.Id == id)
            .SelectMany(u => u.GroupTeachers)
            .Select(gt => gt.Group)
            .ProjectTo<Group>(_mapper.ConfigurationProvider)
            .ToListAsync();
    }

    public async Task<List<Subject>> GetAllSubjectsByIdAsync(long id)
    {
        return await _gradeManagementDbContext.User
            .Where(u => u.Id == id)
            .SelectMany(u => u.SubjectTeachers)
            .Select(st => st.Subject)
            .ProjectTo<Subject>(_mapper.ConfigurationProvider)
            .ToListAsync();
    }
}
