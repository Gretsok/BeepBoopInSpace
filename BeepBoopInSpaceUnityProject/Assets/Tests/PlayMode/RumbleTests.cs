using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.HID;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using static Game.Tests.PlayMode.ScenarioDriver;

namespace Game.Tests.PlayMode
{
    /// <summary>
    /// The rumble of a PS4 controller, on a virtual DualShock 4 whose HID output reports are recorded: over USB, the
    /// Input System's report; over Bluetooth, the report the controller listens to there (0x11, with its CRC).
    /// </summary>
    public unsafe class RumbleTests
    {
        private const string k_GamepadRumble = "Game.Gameplay.CharactersManagement.Rumble.GamepadRumble";
        // The DualShock 4's output reports over USB, and the longest one over Bluetooth, which Windows gives as the size.
        private const int k_UsbOutputReportSize = 32;
        private const int k_BluetoothOutputReportSize = 547;
        // zlib.crc32 of 0xA2 followed by the first 74 bytes of the Bluetooth report, with these motor speeds.
        private const uint k_CrcAtFullAndFifth = 0x273445bd;
        private const uint k_CrcAtRest = 0x89fe8937;

        private readonly List<byte[]> m_reports = new();
        private InputDevice m_device;

        [TearDown]
        public void TearDown()
        {
            InputSystem.onDeviceCommand -= RecordOutputReport;
            if (m_device != null)
                InputSystem.RemoveDevice(m_device);
            m_device = null;
            m_reports.Clear();
        }

        [Test]
        public void DualShock4_RumblesOverBluetoothWithItsBluetoothReport()
        {
            var pad = AddDualShock4(k_BluetoothOutputReportSize);

            SetMotorSpeeds(pad, 1f, 0.2f);
            SetMotorSpeeds(pad, 1f, 0.2f);
            SetMotorSpeeds(pad, 0f, 0f);

            Assert.AreEqual(2, m_reports.Count, "Each change of the speeds, and only a change, should be sent.");
            AssertBluetoothReport(m_reports[0], 255, 51, k_CrcAtFullAndFifth);
            AssertBluetoothReport(m_reports[1], 0, 0, k_CrcAtRest);
        }

        [Test]
        public void DualShock4_RumblesOverUsbWithItsUsbReport()
        {
            var pad = AddDualShock4(k_UsbOutputReportSize);

            SetMotorSpeeds(pad, 1f, 0.2f);

            Assert.AreEqual(1, m_reports.Count, "The speeds should be sent once.");
            var report = m_reports[0];
            Assert.AreEqual(k_UsbOutputReportSize, report.Length, "The USB report should have the size of the controller's.");
            Assert.AreEqual(0x05, report[0], "The USB report should be report 0x05.");
            Assert.AreEqual(51, report[4], "High frequency motor (right).");
            Assert.AreEqual(255, report[5], "Low frequency motor (left).");
        }

        private static void AssertBluetoothReport(byte[] report, byte lowFrequency, byte highFrequency, uint crc)
        {
            Assert.AreEqual(k_BluetoothOutputReportSize, report.Length, "Windows writes reports of the size of the longest one.");
            Assert.AreEqual(0x11, report[0], "The Bluetooth report should be report 0x11.");
            Assert.AreEqual(0xC4, report[1], "Bluetooth HID with a CRC, 4 ms between input reports.");
            Assert.AreEqual(0x01, report[3], "Only the rumble flag: the light bar keeps its color.");
            Assert.AreEqual(highFrequency, report[6], "High frequency motor (right).");
            Assert.AreEqual(lowFrequency, report[7], "Low frequency motor (left).");
            Assert.AreEqual(crc, BitConverter.ToUInt32(report, 74), "The CRC should be zlib's CRC-32 of 0xA2 and the report.");
            for (int i = 78; i < report.Length; i++)
                Assert.AreEqual(0, report[i], $"Byte {i}, past the 78 bytes of the report, should be zero.");
        }

        private Gamepad AddDualShock4(int outputReportSize)
        {
            m_device = InputSystem.AddDevice(new InputDeviceDescription
            {
                interfaceName = "HID",
                product = "Wireless Controller",
                capabilities = new HID.HIDDeviceDescriptor
                {
                    vendorId = 0x54C,
                    productId = 0x9CC,
                    inputReportSize = 64,
                    outputReportSize = outputReportSize,
                }.ToJson(),
            });
            Assert.IsInstanceOf<DualShock4GamepadHID>(m_device, "The virtual controller isn't a DualShock 4.");
            InputSystem.onDeviceCommand += RecordOutputReport;
            return (Gamepad)m_device;
        }

        private long? RecordOutputReport(InputDevice device, InputDeviceCommand* command)
        {
            if (device != m_device || command->type != new FourCC('H', 'I', 'D', 'O'))
                return null;
            var report = new byte[command->payloadSizeInBytes];
            var payload = (byte*)command->payloadPtr;
            for (int i = 0; i < report.Length; i++)
                report[i] = payload[i];
            m_reports.Add(report);
            return report.Length;
        }

        private static void SetMotorSpeeds(Gamepad pad, float lowFrequency, float highFrequency) =>
            GameType(k_GamepadRumble).GetMethod("SetMotorSpeeds").Invoke(null, new object[] { pad, lowFrequency, highFrequency });
    }
}
