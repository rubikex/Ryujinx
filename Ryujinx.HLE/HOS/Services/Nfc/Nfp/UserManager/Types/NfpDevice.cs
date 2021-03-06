﻿using Ryujinx.HLE.HOS.Kernel.Threading;
using Ryujinx.HLE.HOS.Services.Hid;

namespace Ryujinx.HLE.HOS.Services.Nfc.Nfp.UserManager
{
    class NfpDevice
    {
        public KEvent ActivateEvent;
        public KEvent DeactivateEvent;

        public void SignalActivate()   => ActivateEvent.ReadableEvent.Signal();
        public void SignalDeactivate() => DeactivateEvent.ReadableEvent.Signal();

        public NfpDeviceState State = NfpDeviceState.Unavailable;

        public PlayerIndex Handle;
        public NpadIdType  NpadIdType;

        public byte[] AmiiboId;

        public bool UseRandomUuid;
    }
}