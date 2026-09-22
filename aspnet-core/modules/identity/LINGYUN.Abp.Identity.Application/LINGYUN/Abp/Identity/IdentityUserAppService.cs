using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Users;

namespace LINGYUN.Abp.Identity;

[Authorize(Volo.Abp.Identity.IdentityPermissions.Users.Default)]
public class IdentityUserAppService : IdentityAppServiceBase, IIdentityUserAppService
{
    protected IdentityUserManager UserManager { get; }
    protected IIdentityUserRepository UserRepository { get; }
    protected IIdentityRoleRepository RoleRepository { get; }
    protected IRepository<IdentityRole, Guid> RoleGenericRepository { get; }
    protected IOptions<IdentityOptions> IdentityOptions { get; }
    public IdentityUserAppService(
        IRepository<IdentityRole, Guid> roleGenericRepository,
        IdentityUserManager userManager,
        IIdentityUserRepository userRepository,
        IIdentityRoleRepository roleRepository,
        IOptions<IdentityOptions> identityOptions) 
    {
        RoleGenericRepository = roleGenericRepository;
        UserManager = userManager;
        IdentityOptions = identityOptions;
        UserRepository = userRepository;
        RoleRepository = roleRepository;
    }
    
    #region OrganizationUnit

    public async virtual Task<ListResultDto<OrganizationUnitDto>> GetOrganizationUnitsAsync(Guid id)
    {
        var user = await UserManager.GetByIdAsync(id);

        var origanizationUnits = await UserManager.GetOrganizationUnitsAsync(user);

        return new ListResultDto<OrganizationUnitDto>(
            ObjectMapper.Map<List<OrganizationUnit>, List<OrganizationUnitDto>>(origanizationUnits));
    }

    [Authorize(IdentityPermissions.Users.ManageOrganizationUnits)]
    public async virtual Task SetOrganizationUnitsAsync(Guid id, IdentityUserOrganizationUnitUpdateDto input)
    {
        var user = await UserManager.GetByIdAsync(id);

        await UserManager.SetOrganizationUnitsAsync(user, input.OrganizationUnitIds);

        await CurrentUnitOfWork!.SaveChangesAsync();
    }

    [Authorize(IdentityPermissions.Users.ManageOrganizationUnits)]
    public async virtual Task RemoveOrganizationUnitsAsync(Guid id, Guid ouId)
    {
        await UserManager.RemoveFromOrganizationUnitAsync(id, ouId);

        await CurrentUnitOfWork!.SaveChangesAsync();
    }

    #endregion

    #region Claim

    public async virtual Task<ListResultDto<IdentityClaimDto>> GetClaimsAsync(Guid id)
    {
        var user = await UserManager.GetByIdAsync(id);

        return new ListResultDto<IdentityClaimDto>(ObjectMapper.Map<List<IdentityUserClaim>, List<IdentityClaimDto>>(user.Claims.ToList()));
    }

    [Authorize(IdentityPermissions.Users.ManageClaims)]
    public async virtual Task AddClaimAsync(Guid id, IdentityUserClaimCreateDto input)
    {
        var user = await UserManager.GetByIdAsync(id);
        var claim = new Claim(input.ClaimType, input.ClaimValue);
        if (user.FindClaim(claim) != null)
        {
            throw new UserFriendlyException(L["UserClaimAlreadyExists"]);
        }
        user.AddClaim(GuidGenerator, claim);
        (await UserManager.UpdateAsync(user)).CheckErrors();

        await CurrentUnitOfWork!.SaveChangesAsync();
    }

    [Authorize(IdentityPermissions.Users.ManageClaims)]
    public async virtual Task UpdateClaimAsync(Guid id, IdentityUserClaimUpdateDto input)
    {
        var user = await UserManager.GetByIdAsync(id);
        var oldClaim = new Claim(input.ClaimType, input.ClaimValue);
        var newClaim = new Claim(input.ClaimType, input.NewClaimValue);
        user.ReplaceClaim(oldClaim, newClaim);
        (await UserManager.UpdateAsync(user)).CheckErrors();

        await CurrentUnitOfWork!.SaveChangesAsync();
    }

    [Authorize(IdentityPermissions.Users.ManageClaims)]
    public async virtual Task DeleteClaimAsync(Guid id, IdentityUserClaimDeleteDto input)
    {
        var user = await UserManager.GetByIdAsync(id);
        user.RemoveClaim(new Claim(input.ClaimType, input.ClaimValue));
        (await UserManager.UpdateAsync(user)).CheckErrors();

        await CurrentUnitOfWork!.SaveChangesAsync();
    }

    #endregion

    #region Security

    [Authorize(IdentityPermissions.Users.ResetPassword)]
    public async virtual Task ChangePasswordAsync(Guid id, IdentityUserSetPasswordInput input)
    {
        var user = await GetUserAsync(id);

        if (user.IsExternal)
        {
            throw new BusinessException(code: Volo.Abp.Identity.IdentityErrorCodes.ExternalUserPasswordChange);
        }

        if (user.PasswordHash == null)
        {
            (await UserManager.AddPasswordAsync(user, input.Password)).CheckErrors();
        }
        else
        {
            var token = await UserManager.GeneratePasswordResetTokenAsync(user);

            (await UserManager.ResetPasswordAsync(user, token, input.Password)).CheckErrors();
        }

        await CurrentUnitOfWork!.SaveChangesAsync();
    }

    [Authorize(Volo.Abp.Identity.IdentityPermissions.Users.Update)]
    public async virtual Task ChangeTwoFactorEnabledAsync(Guid id, TwoFactorEnabledDto input)
    {
        var user = await GetUserAsync(id);

        (await UserManager.SetTwoFactorEnabledWithAccountConfirmedAsync(user, input.Enabled)).CheckErrors();

        await CurrentUnitOfWork!.SaveChangesAsync();
    }

    #endregion

    #region Lock

    [Authorize(Volo.Abp.Identity.IdentityPermissions.Users.Update)]
    public async virtual Task LockAsync(Guid id, int seconds)
    {
        var user = await GetUserAsync(id);
        //if (!UserManager.SupportsUserLockout)
        //{
        //    throw new UserFriendlyException(L["Volo.Abp.Identity:UserLockoutNotEnabled"]);
        //}
        var endDate = new DateTimeOffset(Clock.Now).AddSeconds(seconds);
        (await UserManager.SetLockoutEndDateAsync(user, endDate)).CheckErrors();

        await CurrentUnitOfWork!.SaveChangesAsync();
    }

    [Authorize(Volo.Abp.Identity.IdentityPermissions.Users.Update)]
    public async virtual Task UnLockAsync(Guid id)
    {
        var user = await GetUserAsync(id);
        (await UserManager.SetLockoutEndDateAsync(user, null)).CheckErrors();

        await CurrentUnitOfWork!.SaveChangesAsync();
    }

    #endregion

    protected async virtual Task<IdentityUser> GetUserAsync(Guid id)
    {
        await IdentityOptions.SetAsync();
        var user = await UserManager.GetByIdAsync(id);

        return user;
    }

    //[Authorize(Volo.Abp.Authorization.Permissions.IdentityPermissions.Users.Default)]
    //public virtual async Task<PagedResultDto<LINGYUN.Abp.Identity.Dto.IdentityUserDto>> GetListAsync(GetIdentityUsersInput input)
    //{
    //    var count = await UserRepository.GetCountAsync(input.Filter);
    //    var list = await UserRepository.GetListAsync(input.Sorting, input.MaxResultCount, input.SkipCount, input.Filter);

    //    return new PagedResultDto<LINGYUN.Abp.Identity.Dto.IdentityUserDto>(
    //        count,
    //        ObjectMapper.Map<List<IdentityUser>, List<LINGYUN.Abp.Identity.Dto.IdentityUserDto>>(list)
    //    );
    //}
    public virtual async Task<PagedResultDto<LINGYUN.Abp.Identity.Dto.IdentityUserDto>> GetUserListWithRoleNameAsync(GetIdentityUsersInput input)
    {
        // 1. 查询总条数（过滤条件不变）
        var count = await UserRepository.GetCountAsync(input.Filter);

        //// 2. 核心：使用 GetQueryable 手动 Include 用户角色，原生 GetListAsync 无法加载导航
        //var query = await UserRepository.GetQueryableAsync();

        //query = query
        //    .WhereIf(!input.Filter.IsNullOrWhiteSpace(), x =>
        //        x.UserName.Contains(input.Filter) ||
        //        x.Email.Contains(input.Filter) ||
        //        x.Name.Contains(input.Filter) ||
        //        x.Surname.Contains(input.Filter))
        //    // 加载用户角色多对多导航
        //    .Include(u => u.Roles)
        //        .ThenInclude(ur => ur.Role);

        //// 分页、排序
        //var userList = await query
        //    .OrderBy(input.Sorting ?? nameof(IdentityUser.UserName))
        //    .Skip(input.SkipCount)
        //    .Take(input.MaxResultCount)
        //    .ToListAsync();

        var userList = await UserRepository.GetListAsync(input.Sorting, input.MaxResultCount, input.SkipCount, input.Filter, includeDetails: true);

        // 取出所有需要用到的 RoleId
        var roleIds = userList
            .SelectMany(u => u.Roles.Select(ur => ur.RoleId))
            .Distinct()
            .ToList(); 

        // 一次性批量查询角色，建立 RoleId -> RoleName 字典
        var roleList = await RoleRepository.GetListByIdListAsync(roleIds);
        var roleDict = roleList.ToDictionary(r => r.Id, r => r.Name);

        var dtoList = ObjectMapper.Map<List<IdentityUser>, List<LINGYUN.Abp.Identity.Dto.IdentityUserDto>>(userList);

        for (int i = 0; i < userList.Count; i++)
        {
            var user = userList[i];
            // 根据RoleId从字典取出角色名称
            dtoList[i].RoleNames = user.Roles
                .Select(ur => roleDict.TryGetValue(ur.RoleId, out var name) ? name : null)
                .Where(name => name != null)
                .ToArray();
        }

        return new PagedResultDto<LINGYUN.Abp.Identity.Dto.IdentityUserDto>(count, dtoList);
    }
}
