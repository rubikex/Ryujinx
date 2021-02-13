﻿using Ryujinx.Common.Configuration;
using Ryujinx.Common.Memory;
using Ryujinx.HLE.HOS.Services.Mii;
using Ryujinx.HLE.HOS.Services.Mii.Types;
using Ryujinx.HLE.HOS.Services.Nfc.Nfp.UserManager;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Ryujinx.HLE.HOS.Services.Nfc.Nfp
{
    static class VirtualAmiibo
    {
        private static uint _openedApplicationAreaId;

        public static byte[] GenerateUuid(byte[] amiiboId, bool useRandomUuid)
        {
            if (useRandomUuid)
            {
                return GenerateRandomUuid();
            }

            VirtualAmiiboFile virtualAmiiboFile = LoadAmiiboFile(amiiboId);

            if (virtualAmiiboFile.TagUuid.Length == 0)
            {
                virtualAmiiboFile.TagUuid = GenerateRandomUuid();

                SaveAmiiboFile(virtualAmiiboFile);
            }

            return virtualAmiiboFile.TagUuid;
        }

        private static byte[] GenerateRandomUuid()
        {
            byte[] uuid = new byte[9];

            new Random().NextBytes(uuid);

            uuid[3] = (byte)(0x88    ^ uuid[0] ^ uuid[1] ^ uuid[2]);
            uuid[8] = (byte)(uuid[3] ^ uuid[4] ^ uuid[5] ^ uuid[6]);

            return uuid;
        }

        public static CommonInfo GetCommonInfo(byte[] amiiboId)
        {
            VirtualAmiiboFile amiiboFile = LoadAmiiboFile(amiiboId);

            return new CommonInfo()
            {
                LastWriteYear       = (ushort)amiiboFile.LastWriteDate.Year,
                LastWriteMonth      = (byte)amiiboFile.LastWriteDate.Month,
                LastWriteDay        = (byte)amiiboFile.LastWriteDate.Day,
                WriteCounter        = amiiboFile.WriteCounter,
                Version             = 1,
                ApplicationAreaSize = AmiiboConstants.ApplicationAreaSize,
                Reserved            = new Array52<byte>()
            };
        }

        public static RegisterInfo GetRegisterInfo(byte[] amiiboId)
        {
            VirtualAmiiboFile amiiboFile = LoadAmiiboFile(amiiboId);

            UtilityImpl utilityImpl = new UtilityImpl();
            CharInfo    charInfo    = new CharInfo();

            charInfo.SetFromStoreData(StoreData.BuildDefault(utilityImpl, 0));

            // TODO: Maybe change the "no name" by the player name when user profile will be implemented.
            // charInfo.Nickname = Nickname.FromString("Nickname");

            RegisterInfo registerInfo = new RegisterInfo()
            {
                MiiCharInfo     = charInfo,
                FirstWriteYear  = (ushort)amiiboFile.FirstWriteDate.Year,
                FirstWriteMonth = (byte)amiiboFile.FirstWriteDate.Month,
                FirstWriteDay   = (byte)amiiboFile.FirstWriteDate.Day,
                FontRegion      = 0,
                Reserved1       = new Array64<byte>(),
                Reserved2       = new Array58<byte>()
            };

            Encoding.ASCII.GetBytes("Ryujinx").CopyTo(registerInfo.Nickname.ToSpan());

            return registerInfo;
        }

        public static bool OpenApplicationArea(byte[] amiiboId, uint applicationAreaId)
        {
            VirtualAmiiboFile virtualAmiiboFile = LoadAmiiboFile(amiiboId);

            if (virtualAmiiboFile.ApplicationAreas.Any(item => item.ApplicationAreaId == applicationAreaId))
            {
                _openedApplicationAreaId = applicationAreaId;

                return true;
            }

            return false;
        }

        public static byte[] GetApplicationArea(byte[] amiiboId)
        {
            VirtualAmiiboFile virtualAmiiboFile = LoadAmiiboFile(amiiboId);

            foreach (VirtualAmiiboApplicationArea applicationArea in virtualAmiiboFile.ApplicationAreas)
            {
                if (applicationArea.ApplicationAreaId == _openedApplicationAreaId)
                {
                    return applicationArea.ApplicationArea;
                }
            }

            return Array.Empty<byte>();
        }

        public static bool CreateApplicationArea(byte[] amiiboId, uint applicationAreaId, byte[] applicationAreaData)
        {
            VirtualAmiiboFile virtualAmiiboFile = LoadAmiiboFile(amiiboId);

            if (virtualAmiiboFile.ApplicationAreas.Any(item => item.ApplicationAreaId == applicationAreaId))
            {
                return false;
            }

            virtualAmiiboFile.ApplicationAreas.Add(new VirtualAmiiboApplicationArea()
            {
                ApplicationAreaId = applicationAreaId,
                ApplicationArea   = applicationAreaData
            });

            SaveAmiiboFile(virtualAmiiboFile);

            return true;
        }

        public static void SetApplicationArea(byte[] amiiboId, byte[] applicationAreaData)
        {
            VirtualAmiiboFile virtualAmiiboFile = LoadAmiiboFile(amiiboId);

            if (virtualAmiiboFile.ApplicationAreas.Any(item => item.ApplicationAreaId == _openedApplicationAreaId))
            {
                for (int i = 0; i < virtualAmiiboFile.ApplicationAreas.Count; i++)
                {
                    if (virtualAmiiboFile.ApplicationAreas[i].ApplicationAreaId == _openedApplicationAreaId)
                    {
                        virtualAmiiboFile.ApplicationAreas[i] = new VirtualAmiiboApplicationArea()
                        {
                            ApplicationAreaId = _openedApplicationAreaId,
                            ApplicationArea   = applicationAreaData
                        };

                        break;
                    }
                }

                SaveAmiiboFile(virtualAmiiboFile);
            }
        }

        private static VirtualAmiiboFile LoadAmiiboFile(byte[] amiiboId)
        {
            Directory.CreateDirectory(Path.Join(AppDataManager.BaseDirPath, "system", "amiibo"));

            string filePath = Path.Join(AppDataManager.BaseDirPath, "system", "amiibo", $"{BitConverter.ToString(amiiboId).Replace("-", "")}.json");

            VirtualAmiiboFile virtualAmiiboFile;

            if (File.Exists(filePath))
            {
                virtualAmiiboFile = JsonSerializer.Deserialize<VirtualAmiiboFile>(File.ReadAllText(filePath));
            }
            else
            {
                virtualAmiiboFile = new VirtualAmiiboFile()
                {
                    FileVersion      = 0,
                    TagUuid          = Array.Empty<byte>(),
                    AmiiboId         = amiiboId,
                    FirstWriteDate   = DateTime.Now,
                    LastWriteDate    = DateTime.Now,
                    WriteCounter     = 0,
                    ApplicationAreas = new List<VirtualAmiiboApplicationArea>()
                };

                SaveAmiiboFile(virtualAmiiboFile);
            }

            return virtualAmiiboFile;
        }

        private static void SaveAmiiboFile(VirtualAmiiboFile virtualAmiiboFile)
        {
            string filePath = Path.Join(AppDataManager.BaseDirPath, "system", "amiibo", $"{BitConverter.ToString(virtualAmiiboFile.AmiiboId).Replace("-", "")}.json");

            File.WriteAllText(filePath, JsonSerializer.Serialize(virtualAmiiboFile));
        }
    }
}