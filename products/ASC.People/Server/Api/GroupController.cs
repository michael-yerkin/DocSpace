﻿// (c) Copyright Ascensio System SIA 2010-2022
//
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

namespace ASC.Employee.Core.Controllers;

[Scope]
[DefaultRoute]
[ApiController]
public class GroupController : ControllerBase
{
    private readonly UserManager _userManager;
    private readonly ApiContext _apiContext;
    private readonly GroupFullDtoHelper _groupFullDtoHelper;
    private readonly MessageService _messageService;
    private readonly MessageTarget _messageTarget;
    private readonly PermissionContext _permissionContext;

    public GroupController(
        UserManager userManager,
        ApiContext apiContext,
        GroupFullDtoHelper groupFullDtoHelper,
        MessageService messageService,
        MessageTarget messageTarget,
        PermissionContext permissionContext)
    {
        _userManager = userManager;
        _apiContext = apiContext;
        _groupFullDtoHelper = groupFullDtoHelper;
        _messageService = messageService;
        _messageTarget = messageTarget;
        _permissionContext = permissionContext;
    }

    [HttpGet]
    public IEnumerable<GroupSummaryDto> GetAll()
    {
        var result = _userManager.GetDepartments().Select(r => r);
        if (!string.IsNullOrEmpty(_apiContext.FilterValue))
        {
            result = result.Where(r => r.Name.Contains(_apiContext.FilterValue, StringComparison.InvariantCultureIgnoreCase));
        }

        return result.Select(x => new GroupSummaryDto(x, _userManager));
    }

    [HttpGet("full")]
    public async IAsyncEnumerable<GroupDto> GetAllWithMembers()
    {
        var result = _userManager.GetDepartments().Select(r => r);
        if (!string.IsNullOrEmpty(_apiContext.FilterValue))
        {
            result = result.Where(r => r.Name.Contains(_apiContext.FilterValue, StringComparison.InvariantCultureIgnoreCase));
        }

        foreach (var item in result)
        {
            yield return await _groupFullDtoHelper.Get(item, true);
        }
    }

    [HttpGet("search")]
    public IEnumerable<GroupSummaryDto> GetTagsByName(string groupName)
    {
        groupName = (groupName ?? "").Trim();

        if (string.IsNullOrEmpty(groupName))
        {
            return new List<GroupSummaryDto>();
        }

        return _userManager.GetDepartments()
            .Where(x => x.Name.Contains(groupName))
            .Select(x => new GroupSummaryDto(x, _userManager));
    }

    [HttpGet("{groupid}")]
    public async Task<GroupDto> GetById(Guid groupid)
    {
        return await _groupFullDtoHelper.Get(GetGroupInfo(groupid), true);
    }

    [HttpGet("user/{userid}")]
    public IEnumerable<GroupSummaryDto> GetByUserId(Guid userid)
    {
        return _userManager.GetUserGroups(userid).Select(x => new GroupSummaryDto(x, _userManager));
    }

    [HttpPost]
    public async Task<GroupDto> AddGroup(GroupRequestDto inDto)
    {
        _permissionContext.DemandPermissions(Constants.Action_EditGroups, Constants.Action_AddRemoveUser);

        var group = _userManager.SaveGroupInfo(new GroupInfo { Name = inDto.GroupName });

        await TransferUserToDepartment(inDto.GroupManager, @group, true);

        if (inDto.Members != null)
        {
            foreach (var member in inDto.Members)
            {
                await TransferUserToDepartment(member, group, false);
            }
        }

        _messageService.Send(MessageAction.GroupCreated, _messageTarget.Create(group.ID), group.Name);

        return await _groupFullDtoHelper.Get(group, true);
    }

    [HttpPut("{groupid}")]
    public async Task<GroupDto> UpdateGroup(Guid groupid, GroupRequestDto inDto)
    {
        _permissionContext.DemandPermissions(Constants.Action_EditGroups, Constants.Action_AddRemoveUser);
        var group = _userManager.GetGroups().SingleOrDefault(x => x.ID == groupid).NotFoundIfNull("group not found");
        if (groupid == Constants.LostGroupInfo.ID)
        {
            throw new ItemNotFoundException("group not found");
        }

        group.Name = inDto.GroupName ?? group.Name;
        _userManager.SaveGroupInfo(group);

        await RemoveMembersFrom(groupid, new GroupRequestDto { Members = _userManager.GetUsersByGroup(groupid, EmployeeStatus.All).Select(u => u.Id).Where(id => !inDto.Members.Contains(id)) });

        await TransferUserToDepartment(inDto.GroupManager, @group, true);

        if (inDto.Members != null)
        {
            foreach (var member in inDto.Members)
            {
                await TransferUserToDepartment(member, group, false);
            }
        }

        _messageService.Send(MessageAction.GroupUpdated, _messageTarget.Create(groupid), group.Name);

        return await GetById(groupid);
    }

    [HttpDelete("{groupid}")]
    public async Task<GroupDto> DeleteGroup(Guid groupid)
    {
        _permissionContext.DemandPermissions(Constants.Action_EditGroups, Constants.Action_AddRemoveUser);

        var @group = GetGroupInfo(groupid);

        _userManager.DeleteGroup(groupid);

        _messageService.Send(MessageAction.GroupDeleted, _messageTarget.Create(group.ID), group.Name);

        return await _groupFullDtoHelper.Get(group, false);
    }

    [HttpPut("{groupid}/members/{newgroupid}")]
    public async Task<GroupDto> TransferMembersTo(Guid groupid, Guid newgroupid)
    {
        _permissionContext.DemandPermissions(Constants.Action_EditGroups, Constants.Action_AddRemoveUser);

        var oldgroup = GetGroupInfo(groupid);

        var newgroup = GetGroupInfo(newgroupid);

        var users = _userManager.GetUsersByGroup(oldgroup.ID);
        foreach (var userInfo in users)
        {
            await TransferUserToDepartment(userInfo.Id, newgroup, false);
        }

        return await GetById(newgroupid);
    }

    [HttpPost("{groupid}/members")]
    public async Task<GroupDto> SetMembersTo(Guid groupid, GroupRequestDto inDto)
    {
        await RemoveMembersFrom(groupid, new GroupRequestDto { Members = _userManager.GetUsersByGroup(groupid).Select(x => x.Id) });
        await AddMembersTo(groupid, inDto);

        return await GetById(groupid);
    }

    [HttpPut("{groupid}/members")]
    public async Task<GroupDto> AddMembersTo(Guid groupid, GroupRequestDto inDto)
    {
        _permissionContext.DemandPermissions(Constants.Action_EditGroups, Constants.Action_AddRemoveUser);

        var group = GetGroupInfo(groupid);

        foreach (var userId in inDto.Members)
        {
            await TransferUserToDepartment(userId, group, false);
        }

        return await GetById(group.ID);
    }

    [HttpPut("{groupid}/manager")]
    public async Task<GroupDto> SetManager(Guid groupid, SetManagerRequestDto inDto)
    {
        var group = GetGroupInfo(groupid);
        if (_userManager.UserExists(inDto.UserId))
        {
            _userManager.SetDepartmentManager(group.ID, inDto.UserId);
        }
        else
        {
            throw new ItemNotFoundException("user not found");
        }

        return await GetById(groupid);
    }

    [HttpDelete("{groupid}/members")]
    public async Task<GroupDto> RemoveMembersFrom(Guid groupid, GroupRequestDto inDto)
    {
        _permissionContext.DemandPermissions(Constants.Action_EditGroups, Constants.Action_AddRemoveUser);

        var group = GetGroupInfo(groupid);

        foreach (var userId in inDto.Members)
        {
            await RemoveUserFromDepartment(userId, group);
        }

        return await GetById(group.ID);
    }

    private GroupInfo GetGroupInfo(Guid groupid)
    {
        var group = _userManager.GetGroups().SingleOrDefault(x => x.ID == groupid).NotFoundIfNull("group not found");
        if (group.ID == Constants.LostGroupInfo.ID)
        {
            throw new ItemNotFoundException("group not found");
        }

        return @group;
    }

    private async Task TransferUserToDepartment(Guid userId, GroupInfo group, bool setAsManager)
    {
        if (!_userManager.UserExists(userId) && userId != Guid.Empty)
        {
            return;
        }

        if (setAsManager)
        {
            _userManager.SetDepartmentManager(@group.ID, userId);
        }
        await _userManager.AddUserIntoGroup(userId, @group.ID);
    }

    private async Task RemoveUserFromDepartment(Guid userId, GroupInfo @group)
    {
        if (!_userManager.UserExists(userId))
        {
            return;
        }

        var user = _userManager.GetUsers(userId);
        await _userManager.RemoveUserFromGroup(user.Id, @group.ID);
        await _userManager.UpdateUserInfo(user);
    }
}
