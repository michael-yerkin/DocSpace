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

using Constants = ASC.Core.Users.Constants;

namespace ASC.Web.Core.Quota;

public class CountUserChecker : TenantQuotaFeatureCheckerCount<CountUserFeature>
{
    private readonly ITariffService _tariffService;
    public override string Exception => Resource.TariffsFeature_users_exception;

    public CountUserChecker(ITenantQuotaFeatureStat<CountUserFeature, int> tenantQuotaFeatureStatistic, TenantManager tenantManager, ITariffService tariffService)
        : base(tenantQuotaFeatureStatistic, tenantManager)
    {
        _tariffService = tariffService;
    }

    public override void CheckAdd(int tenantId, int newValue)
    {
        if (_tariffService.GetTariff(tenantId).State > TariffState.Paid)
        {
            throw new BillingNotFoundException(Resource.ErrorNotAllowedOption, "users");
        }

        base.CheckAdd(tenantId, newValue);
    }
}

public class CountUserStatistic : ITenantQuotaFeatureStat<CountUserFeature, int>
{
    private readonly IServiceProvider _serviceProvider;

    public CountUserStatistic(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task<int> GetValue()
    {
        var userManager = _serviceProvider.GetService<UserManager>();
        return Task.FromResult(userManager.GetUsersByGroup(Constants.GroupUser.ID).Length);
    }
}