// (c) Copyright Ascensio System SIA 2010-2022
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

namespace ASC.Web.Studio.Core.Notify;

[Scope]
public class StudioNotifyHelper
{
    public readonly string Helplink;
    public readonly StudioNotifySource NotifySource;
    public readonly ISubscriptionProvider SubscriptionProvider;
    public readonly IRecipientProvider RecipientsProvider;

    private readonly int _countMailsToNotActivated;
    private readonly string _notificationImagePath;
    private readonly UserManager _userManager;
    private readonly SettingsManager _settingsManager;
    private readonly CommonLinkUtility _commonLinkUtility;
    private readonly TenantManager _tenantManager;
    private readonly TenantExtra _tenantExtra;
    private readonly CoreBaseSettings _coreBaseSettings;
    private readonly WebImageSupplier _webImageSupplier;
    private readonly ILogger<StudioNotifyHelper> _logger;

    public StudioNotifyHelper(
        StudioNotifySource studioNotifySource,
        UserManager userManager,
        SettingsManager settingsManager,
        AdditionalWhiteLabelSettingsHelperInit additionalWhiteLabelSettingsHelper,
        CommonLinkUtility commonLinkUtility,
        TenantManager tenantManager,
        TenantExtra tenantExtra,
        CoreBaseSettings coreBaseSettings,
        WebImageSupplier webImageSupplier,
        IConfiguration configuration,
        ILogger<StudioNotifyHelper> logger)
    {
        Helplink = commonLinkUtility.GetHelpLink(settingsManager, additionalWhiteLabelSettingsHelper, false);
        NotifySource = studioNotifySource;
        _userManager = userManager;
        _settingsManager = settingsManager;
        _commonLinkUtility = commonLinkUtility;
        _tenantManager = tenantManager;
        _tenantExtra = tenantExtra;
        _coreBaseSettings = coreBaseSettings;
        _webImageSupplier = webImageSupplier;
        SubscriptionProvider = NotifySource.GetSubscriptionProvider();
        RecipientsProvider = NotifySource.GetRecipientsProvider();
        _logger = logger;

        int.TryParse(configuration["core:notify:countspam"], out _countMailsToNotActivated);
        _notificationImagePath = configuration["web:notification:image:path"];
    }


    public IEnumerable<UserInfo> GetRecipients(bool toadmins, bool tousers, bool toguests)
    {
        if (toadmins)
        {
            if (tousers)
            {
                if (toguests)
                {
                    return _userManager.GetUsers();
                }

                return _userManager.GetUsers(EmployeeStatus.Default, EmployeeType.RoomAdmin);
            }

            if (toguests)
            {
                return
                    _userManager.GetUsersByGroup(Constants.GroupAdmin.ID)
                               .Concat(_userManager.GetUsers(EmployeeStatus.Default, EmployeeType.User));
            }

            return _userManager.GetUsersByGroup(Constants.GroupAdmin.ID);
        }

        if (tousers)
        {
            if (toguests)
            {
                return _userManager.GetUsers()
                                  .Where(u => !_userManager.IsUserInGroup(u.Id, Constants.GroupAdmin.ID));
            }

            return _userManager.GetUsers(EmployeeStatus.Default, EmployeeType.RoomAdmin)
                              .Where(u => !_userManager.IsUserInGroup(u.Id, Constants.GroupAdmin.ID));
        }

        if (toguests)
        {
            return _userManager.GetUsers(EmployeeStatus.Default, EmployeeType.User);
        }

        return new List<UserInfo>();
    }

    public IRecipient ToRecipient(Guid userId)
    {
        return RecipientsProvider.GetRecipient(userId.ToString());
    }

    public IRecipient[] RecipientFromEmail(string email, bool checkActivation)
    {
        return RecipientFromEmail(new List<string> { email }, checkActivation);
    }

    public IRecipient[] RecipientFromEmail(List<string> emails, bool checkActivation)
    {
        var res = new List<IRecipient>();

        if (emails == null)
        {
            return res.ToArray();
        }

        res.AddRange(emails.
                         Select(email => email.ToLower()).
                         Select(e => new DirectRecipient(e, null, new[] { e }, checkActivation)));

        if (!checkActivation
            && _countMailsToNotActivated > 0
            && _tenantExtra.Saas && !_coreBaseSettings.Personal)
        {
            var tenant = _tenantManager.GetCurrentTenant();
            var tariff = _tenantManager.GetTenantQuota(tenant.Id);
            if (tariff.Free || tariff.Trial)
            {
                var spamEmailSettings = _settingsManager.Load<SpamEmailSettings>();
                var sended = spamEmailSettings.MailsSended;

                var mayTake = Math.Max(0, _countMailsToNotActivated - sended);
                var tryCount = res.Count;
                if (mayTake < tryCount)
                {
                    res = res.Take(mayTake).ToList();

                    _logger.WarningFreeTenant(tenant.Id, tryCount, mayTake);
                }
                spamEmailSettings.MailsSended = sended + tryCount;
                _settingsManager.Save(spamEmailSettings);
            }
        }

        return res.ToArray();
    }

    public string GetNotificationImageUrl(string imageFileName)
    {
        if (string.IsNullOrEmpty(_notificationImagePath))
        {
            return
                _commonLinkUtility.GetFullAbsolutePath(
                    _webImageSupplier.GetAbsoluteWebPath("notifications/" + imageFileName));
        }

        return _notificationImagePath.TrimEnd('/') + "/" + imageFileName;
    }


    public bool IsSubscribedToNotify(Guid userId, INotifyAction notifyAction)
    {
        return IsSubscribedToNotify(ToRecipient(userId), notifyAction);
    }

    public bool IsSubscribedToNotify(IRecipient recipient, INotifyAction notifyAction)
    {
        return recipient != null && SubscriptionProvider.IsSubscribed(_logger, notifyAction, recipient, null);
    }

    public void SubscribeToNotify(Guid userId, INotifyAction notifyAction, bool subscribe)
    {
        SubscribeToNotify(ToRecipient(userId), notifyAction, subscribe);
    }

    public void SubscribeToNotify(IRecipient recipient, INotifyAction notifyAction, bool subscribe)
    {
        if (recipient == null)
        {
            return;
        }

        if (subscribe)
        {
            SubscriptionProvider.Subscribe(notifyAction, null, recipient);
        }
        else
        {
            SubscriptionProvider.UnSubscribe(notifyAction, null, recipient);
        }
    }
}
