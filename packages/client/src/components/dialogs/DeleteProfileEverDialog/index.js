import React from "react";
import { withRouter } from "react-router";
import PropTypes from "prop-types";

import Button from "@docspace/components/button";
import ModalDialog from "@docspace/components/modal-dialog";
import Text from "@docspace/components/text";

import { withTranslation, Trans } from "react-i18next";
import api from "@docspace/common/api";
import toastr from "@docspace/components/toast/toastr";
import ModalDialogContainer from "../ModalDialogContainer";
import { inject, observer } from "mobx-react";
import config from "PACKAGE_FILE";
import { combineUrl } from "@docspace/common/utils";

const { deleteUser } = api.people; //TODO: Move to action
const { Filter } = api;

class DeleteProfileEverDialogComponent extends React.Component {
  constructor(props) {
    super(props);

    this.state = {
      isRequestRunning: false,
    };
  }
  onDeleteProfileEver = () => {
    const { user, t, history, homepage, setFilter, onClose } = this.props;

    const filter = Filter.getDefault();
    const params = filter.toUrlParams();

    const url = combineUrl(
      window.DocSpaceConfig?.proxy?.url,
      homepage,
      `/accounts/filter?${params}`
    );

    this.setState({ isRequestRunning: true }, () => {
      deleteUser(user.id)
        .then((res) => {
          toastr.success(t("SuccessfullyDeleteUserInfoMessage"));
          history.push(url, params);
          setFilter(filter);
          return;
        })
        .catch((error) => toastr.error(error))
        .finally(() => onClose());
    });
  };

  render() {
    console.log("DeleteProfileEverDialog render");
    const { t, tReady, visible, user, onClose, userCaption } = this.props;
    const { isRequestRunning } = this.state;

    return (
      <ModalDialogContainer
        isLoading={!tReady}
        visible={visible}
        onClose={onClose}
      >
        <ModalDialog.Header>{t("DeleteUser")}</ModalDialog.Header>
        <ModalDialog.Body>
          <Text>
            <Trans
              i18nKey="DeleteUserMessage"
              ns="DeleteProfileEverDialog"
              t={t}
            >
              {{ userCaption: t("Common:User") }}{" "}
              <strong>{{ user: user.displayName }}</strong>
              will be deleted. User personal documents which are available to
              others will be deleted.
            </Trans>
          </Text>
        </ModalDialog.Body>
        <ModalDialog.Footer>
          <Button
            key="OKBtn"
            label={t("Common:Delete")}
            size="normal"
            primary={true}
            scale
            onClick={this.onDeleteProfileEver}
            isLoading={isRequestRunning}
          />
          <Button
            label={t("Common:CancelButton")}
            size="normal"
            scale
            onClick={onClose}
          />
        </ModalDialog.Footer>
      </ModalDialogContainer>
    );
  }
}

const DeleteProfileEverDialog = withTranslation([
  "DeleteProfileEverDialog",
  "Common",
  "PeopleTranslations",
])(DeleteProfileEverDialogComponent);

DeleteProfileEverDialog.propTypes = {
  visible: PropTypes.bool.isRequired,
  onClose: PropTypes.func.isRequired,
  user: PropTypes.object.isRequired,
};

export default withRouter(
  inject(({ peopleStore }) => ({
    homepage: config.homepage,
    setFilter: peopleStore.filterStore.setFilterParams,
  }))(observer(DeleteProfileEverDialog))
);
