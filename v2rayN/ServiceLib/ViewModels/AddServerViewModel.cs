using System.Reactive;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ServiceLib.ViewModels;

public class AddServerViewModel : MyReactiveObject
{
    [Reactive]
    public ProfileItem SelectedSource { get; set; }

    [Reactive]
    public string? CoreType { get; set; }

    public ReactiveCommand<Unit, Unit> SaveCmd { get; }

    public AddServerViewModel(ProfileItem profileItem, Func<EViewAction, object?, Task<bool>>? updateView)
    {
        _config = AppHandler.Instance.Config;
        _updateView = updateView;

        SaveCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await SaveServerAsync();
        });

        if (profileItem.IndexId.IsNullOrEmpty())
        {
            profileItem.Network = Global.DefaultNetwork;
            profileItem.HeaderType = Global.None;
            profileItem.RequestHost = "";
            profileItem.StreamSecurity = "";
            SelectedSource = profileItem;
        }
        else
        {
            SelectedSource = JsonUtils.DeepCopy(profileItem);
        }
        CoreType = SelectedSource?.CoreType?.ToString();
    }

    private async Task SaveServerAsync()
    {
        if (SelectedSource.Remarks.IsNullOrEmpty())
        {
            NoticeHandler.Instance.Enqueue(ResUI.PleaseFillRemarks);
            return;
        }

        if (SelectedSource.Address.IsNullOrEmpty())
        {
            NoticeHandler.Instance.Enqueue(ResUI.FillServerAddress);
            return;
        }
        var port = SelectedSource.Port.ToString();
        if (port.IsNullOrEmpty() || !Utils.IsNumeric(port)
            || SelectedSource.Port <= 0 || SelectedSource.Port >= Global.MaxPort)
        {
            NoticeHandler.Instance.Enqueue(ResUI.FillCorrectServerPort);
            return;
        }
        if (SelectedSource.ConfigType == EConfigType.Shadowsocks)
        {
            if (SelectedSource.Id.IsNullOrEmpty())
            {
                NoticeHandler.Instance.Enqueue(ResUI.FillPassword);
                return;
            }
            if (SelectedSource.Security.IsNullOrEmpty())
            {
                NoticeHandler.Instance.Enqueue(ResUI.PleaseSelectEncryption);
                return;
            }
        }
        if (SelectedSource.ConfigType is not EConfigType.SOCKS and not EConfigType.HTTP)
        {
            if (SelectedSource.Id.IsNullOrEmpty())
            {
                NoticeHandler.Instance.Enqueue(ResUI.FillUUID);
                return;
            }
        }
        SelectedSource.CoreType = CoreType.IsNullOrEmpty() ? null : (ECoreType)Enum.Parse(typeof(ECoreType), CoreType);

        if (await ConfigHandler.AddServer(_config, SelectedSource) == 0)
        {
            NoticeHandler.Instance.Enqueue(ResUI.OperationSuccess);
            _updateView?.Invoke(EViewAction.CloseWindow, null);
        }
        else
        {
            NoticeHandler.Instance.Enqueue(ResUI.OperationFailed);
        }
    }
}
