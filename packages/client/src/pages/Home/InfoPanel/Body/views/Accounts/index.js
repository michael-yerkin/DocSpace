import React from "react";
import { inject, observer } from "mobx-react";
import { withTranslation } from "react-i18next";

import withLoader from "@docspace/client/src/HOCs/withLoader";
import Loaders from "@docspace/common/components/Loaders";

import Text from "@docspace/components/text";
import ComboBox from "@docspace/components/combobox";

import { getUserStatus } from "SRC_DIR/helpers/people-helpers";
import { StyledAccountContent } from "../../styles/accounts";

const Accounts = ({
  t,
  selection,
  isOwner,
  isAdmin,
  changeUserType,
  canChangeUserType,
  setSelection,
  getPeopleListItem,
}) => {
  const [statusLabel, setStatusLabel] = React.useState("");
  const [isLoading, setIsLoading] = React.useState(false);

  const { role, id, isVisitor, isCollaborator } = selection;

  React.useEffect(() => {
    getStatusLabel();
  }, [selection, getStatusLabel]);

  const getStatusLabel = React.useCallback(() => {
    const status = getUserStatus(selection);
    switch (status) {
      case "active":
        return setStatusLabel(t("Common:Active"));
      case "pending":
        return setStatusLabel(t("PeopleTranslations:PendingTitle"));
      case "disabled":
        return setStatusLabel(t("Settings:Disabled"));
      default:
        return setStatusLabel(t("Common:Active"));
    }
  }, [selection]);

  const getUserTypeLabel = React.useCallback((role) => {
    switch (role) {
      case "owner":
        return t("Common:Owner");
      case "admin":
        return t("Common:DocSpaceAdmin");
      case "manager":
        return t("Common:RoomAdmin");
      case "collaborator":
        return t("Common:PowerUser");
      case "user":
        return t("Common:User");
    }
  }, []);

  const getTypesOptions = React.useCallback(() => {
    const options = [];

    const adminOption = {
      id: "info-account-type_docspace-admin",
      key: "admin",
      title: t("Common:DocSpaceAdmin"),
      label: t("Common:DocSpaceAdmin"),
      action: "admin",
    };
    const managerOption = {
      id: "info-account-type_room-admin",
      key: "manager",
      title: t("Common:RoomAdmin"),
      label: t("Common:RoomAdmin"),
      action: "manager",
    };
    const collaboratorOption = {
      id: "info-account-type_collaborator",
      key: "collaborator",
      title: t("Common:PowerUser"),
      label: t("Common:PowerUser"),
      action: "collaborator",
    };
    const userOption = {
      id: "info-account-type_user",
      key: "user",
      title: t("Common:User"),
      label: t("Common:User"),
      action: "user",
    };

    isOwner && options.push(adminOption);

    options.push(managerOption);

    if (isVisitor || isCollaborator) options.push(collaboratorOption);

    isVisitor && options.push(userOption);

    return options;
  }, [t, isAdmin, isOwner, isVisitor, isCollaborator]);

  const onAbort = () => {
    setIsLoading(false);
  };

  const onSuccess = (users) => {
    if (users) {
      const items = [];
      users.map((u) => items.push(getPeopleListItem(u)));
      if (items.length === 1) {
        setSelection(getPeopleListItem(items[0]));
      } else {
        setSelection(items);
      }
    }
    setIsLoading(false);
  };

  const onTypeChange = React.useCallback(
    ({ action }) => {
      setIsLoading(true);
      if (!changeUserType(action, [selection], onSuccess, onAbort)) {
        setIsLoading(false);
      }
    },
    [selection, changeUserType, t]
  );

  const typeLabel = getUserTypeLabel(role);

  const renderTypeData = () => {
    const typesOptions = getTypesOptions();

    const combobox = (
      <ComboBox
        id="info-account-type-select"
        className="type-combobox"
        selectedOption={
          typesOptions.find((option) => option.key === role) || {}
        }
        options={typesOptions}
        onSelect={onTypeChange}
        scaled={false}
        size="content"
        displaySelectedOption
        modernView
        manualWidth={"fit-content"}
        isLoading={isLoading}
      />
    );

    const text = (
      <Text
        type="page"
        title={typeLabel}
        fontSize="13px"
        fontWeight={600}
        truncate
        noSelect
      >
        {typeLabel}
      </Text>
    );

    const status = getUserStatus(selection);

    const canChange = canChangeUserType({ ...selection, statusType: status });

    return canChange ? combobox : text;
  };

  const typeData = renderTypeData();

  const statusText = isVisitor ? t("SmartBanner:Price") : t("Common:Paid");

  return (
    <>
      <StyledAccountContent>
        <div className="data__header">
          <Text className={"header__text"} noSelect title={t("Data")}>
            {t("InfoPanel:Data")}
          </Text>
        </div>
        <div className="data__body">
          <Text className={"info_field first-row"} noSelect title={t("Data")}>
            {t("ConnectDialog:Account")}
          </Text>
          <Text
            className={"info_data first-row"}
            fontSize={"13px"}
            fontWeight={600}
            noSelect
            title={statusLabel}
          >
            {statusLabel}
          </Text>

          <Text className={"info_field"} noSelect title={t("Common:Type")}>
            {t("Common:Type")}
          </Text>
          {typeData}

          <Text className={"info_field"} noSelect title={t("UserStatus")}>
            {t("UserStatus")}
          </Text>
          <Text
            className={"info_data first-row"}
            fontSize={"13px"}
            fontWeight={600}
            noSelect
            title={statusLabel}
          >
            {statusText}
          </Text>
          {/* <Text className={"info_field"} noSelect title={t("Common:Room")}>
            {t("Common:Room")}
          </Text>
          <div>Rooms list</div> */}
        </div>
      </StyledAccountContent>
    </>
  );
};

export default inject(({ auth, peopleStore, accessRightsStore }) => {
  const { isOwner, isAdmin, id: selfId } = auth.userStore.user;
  const { changeType: changeUserType, usersStore } = peopleStore;
  const { canChangeUserType } = accessRightsStore;

  const { setSelection } = auth.infoPanelStore;

  return {
    isOwner,
    isAdmin,
    changeUserType,
    selfId,
    canChangeUserType,
    loading: usersStore.operationRunning,
    getPeopleListItem: usersStore.getPeopleListItem,
    setSelection,
  };
})(
  withTranslation([
    "People",
    "InfoPanel",
    "ConnectDialog",
    "Common",
    "PeopleTranslations",
    "People",
    "Settings",
    "SmartBanner",
    "DeleteProfileEverDialog",
    "Translations",
  ])(
    withLoader(observer(Accounts))(
      <Loaders.InfoPanelViewLoader view="accounts" />
    )
  )
);
