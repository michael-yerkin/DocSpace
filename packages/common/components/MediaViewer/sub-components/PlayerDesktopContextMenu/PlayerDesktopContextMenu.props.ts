interface PlayerDesktopContextMenuProps {
  generateContextMenu: (
    isOpen: boolean,
    right?: string,
    bottom?: string
  ) => JSX.Element;
  isPreviewFile: boolean;
  hideContextMenu: boolean;
  onDownloadClick: VoidFunction;
}

export default PlayerDesktopContextMenuProps;
