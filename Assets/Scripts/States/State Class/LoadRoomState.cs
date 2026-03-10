using SimpleFileBrowser;
using System.IO;

public class LoadRoomState : AbsAppState
{
    readonly RoomBuilderManager _rbm;
    public LoadRoomState(StateManager manager, AppActions input, RoomBuilderManager rbm) : base(manager, input)
    {
        _rbm = rbm;
    }

    public override void Enter()
    {
        // Clean meta files
        CleanMetaFiles();

        // Filters
        FileBrowser.SetFilters(false, new FileBrowser.Filter("Room", ".room"));
        _ = FileBrowser.SetDefaultFilter(".room");

        // 3. Apri il Dialogo (Usa le Callback, niente Coroutine!)
        // Parametri: OnSuccess, OnCancel, Mode, Multiple, Path, File, Title, Button
        _ = FileBrowser.ShowLoadDialog(
            OnSuccess,
            OnCancel,
            FileBrowser.PickMode.Files,
            false,
            RoomManagementTools.roomsFolderPath,
            null,
            "Select the room to edit",
            "Load"
        );
    }

    public override void Exit()
    {
        FileBrowser.HideDialog();
    }

    public override void UpdateState() { }

    private void OnSuccess(string[] paths)
    {
        if (paths.Length == 0) return;

        string filePath = paths[0];
        string roomName = Path.GetFileNameWithoutExtension(filePath);

        RoomManagementTools.CreateDTRoom(roomName);

        _rbm.RoomName = roomName;
        _manager.ChangeState(_manager.RoomEditor);
    }

    private void OnCancel()
    {
        _manager.ChangeState(_manager.MainMenu);
    }

    private void CleanMetaFiles()
    {
        try
        {
            System.Collections.Generic.IEnumerable<string> metaFiles = Directory.EnumerateFiles(RoomManagementTools.roomsFolderPath, "*.meta", SearchOption.AllDirectories);
            foreach (string file in metaFiles) File.Delete(file);
        }
        catch { /* Ignore error */ }
    }
}