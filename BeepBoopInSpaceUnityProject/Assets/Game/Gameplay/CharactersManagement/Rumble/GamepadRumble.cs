using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.HID;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace Game.Gameplay.CharactersManagement.Rumble
{
    /// <summary>
    /// Sets the motors of a gamepad. The Input System only sends the DualShock 4's USB report, which the controller
    /// ignores over Bluetooth: a PS4 controller connected by Bluetooth gets its Bluetooth report instead.
    /// </summary>
    public static class GamepadRumble
    {
        public static void SetMotorSpeeds(Gamepad gamepad, float lowFrequency, float highFrequency)
        {
            if (gamepad is DualShock4GamepadHID dualShock4 && DualShock4Bluetooth.TrySetMotorSpeeds(dualShock4, lowFrequency, highFrequency))
                return;
            gamepad.SetMotorSpeeds(lowFrequency, highFrequency);
        }
    }

    /// <summary>
    /// The DualShock 4's Bluetooth output report: 0x11, 78 bytes ending with a CRC-32 (as SDL and Linux's hid-sony
    /// send it). Over USB, the controller's output reports are 32 bytes long; over Bluetooth, Windows gives the size of
    /// the longest one, 547 bytes, which a write must have.
    /// </summary>
    public static class DualShock4Bluetooth
    {
        public const int k_ReportSize = 78;
        public const int k_MaxOutputReportSize = 547;
        private const int k_CrcOffset = k_ReportSize - 4;
        private const byte k_ReportId = 0x11;
        // Bluetooth HID with a CRC, 4 ms between the input reports, like the drivers.
        private const byte k_Mode = 0xC4;
        // Only the rumble: the light bar keeps its color.
        private const byte k_RumbleFlag = 0x01;
        // A Bluetooth HID output report starts with this header byte, which the CRC covers.
        private const byte k_OutputHeader = 0xA2;

        private static readonly Dictionary<int, int> s_outputReportSizes = new();
        private static readonly Dictionary<int, (byte Low, byte High)> s_sentSpeeds = new();

        /// <summary>Sends the speeds in a Bluetooth report, if the controller is connected by Bluetooth.</summary>
        public static bool TrySetMotorSpeeds(DualShock4GamepadHID gamepad, float lowFrequency, float highFrequency)
        {
            var outputReportSize = OutputReportSizeOf(gamepad);
            if (outputReportSize < k_ReportSize || outputReportSize > k_MaxOutputReportSize)
                return false;

            var speeds = (ToByte(lowFrequency), ToByte(highFrequency));
            // A rumble sets the motors every frame: Bluetooth only hears of a change.
            if (s_sentSpeeds.TryGetValue(gamepad.deviceId, out var sent) && sent == speeds)
                return true;

            var report = CreateReport(outputReportSize, speeds.Item1, speeds.Item2);
            if (gamepad.ExecuteCommand(ref report) >= 0)
                s_sentSpeeds[gamepad.deviceId] = speeds;
            return true;
        }

        public static OutputReport CreateReport(int outputReportSize, byte lowFrequency, byte highFrequency)
        {
            var bytes = new byte[k_CrcOffset];
            bytes[0] = k_ReportId;
            bytes[1] = k_Mode;
            bytes[3] = k_RumbleFlag;
            bytes[6] = highFrequency;
            bytes[7] = lowFrequency;
            var crc = Crc32(bytes);

            return new OutputReport
            {
                baseCommand = new InputDeviceCommand(OutputReport.Type, InputDeviceCommand.BaseCommandSize + outputReportSize),
                reportId = bytes[0],
                mode = bytes[1],
                flags = bytes[3],
                highFrequencyMotorSpeed = highFrequency,
                lowFrequencyMotorSpeed = lowFrequency,
                crc0 = (byte)crc,
                crc1 = (byte)(crc >> 8),
                crc2 = (byte)(crc >> 16),
                crc3 = (byte)(crc >> 24),
            };
        }

        private static int OutputReportSizeOf(InputDevice device)
        {
            if (!s_outputReportSizes.TryGetValue(device.deviceId, out var size))
            {
                var capabilities = device.description.capabilities;
                size = string.IsNullOrEmpty(capabilities) ? 0 : HID.HIDDeviceDescriptor.FromJson(capabilities).outputReportSize;
                s_outputReportSizes[device.deviceId] = size;
            }
            return size;
        }

        private static byte ToByte(float speed) => (byte)Mathf.Clamp(Mathf.RoundToInt(speed * 255f), 0, 255);

        // CRC-32 (zlib's) of the header byte followed by the report.
        private static uint Crc32(byte[] report)
        {
            var crc = Crc32Step(0xFFFFFFFFu, k_OutputHeader);
            foreach (var value in report)
                crc = Crc32Step(crc, value);
            return ~crc;
        }

        private static uint Crc32Step(uint crc, byte value)
        {
            crc ^= value;
            for (int bit = 0; bit < 8; bit++)
                crc = (crc & 1u) != 0 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
            return crc;
        }

        /// <summary>The HID output command: the report, then zeros up to the size Windows expects.</summary>
        [StructLayout(LayoutKind.Explicit, Size = InputDeviceCommand.BaseCommandSize + k_MaxOutputReportSize)]
        public struct OutputReport : IInputDeviceCommandInfo
        {
            public static FourCC Type => new FourCC('H', 'I', 'D', 'O');
            private const int k_Payload = InputDeviceCommand.BaseCommandSize;

            [FieldOffset(0)] public InputDeviceCommand baseCommand;
            [FieldOffset(k_Payload + 0)] public byte reportId;
            [FieldOffset(k_Payload + 1)] public byte mode;
            [FieldOffset(k_Payload + 3)] public byte flags;
            [FieldOffset(k_Payload + 6)] public byte highFrequencyMotorSpeed;
            [FieldOffset(k_Payload + 7)] public byte lowFrequencyMotorSpeed;
            [FieldOffset(k_Payload + k_CrcOffset + 0)] public byte crc0;
            [FieldOffset(k_Payload + k_CrcOffset + 1)] public byte crc1;
            [FieldOffset(k_Payload + k_CrcOffset + 2)] public byte crc2;
            [FieldOffset(k_Payload + k_CrcOffset + 3)] public byte crc3;

            public FourCC typeStatic => Type;
        }
    }
}
