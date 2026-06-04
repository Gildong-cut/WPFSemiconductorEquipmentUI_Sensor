using System;

namespace WPFSemiconductorEquipmentUI_Sensor.Services
{
    public interface ITrainerClient : IDisposable
    {
        bool IsConnected { get; }

        void Connect();

        SensorTrainerSnapshot ReadSnapshot();

        void SetRunningLamp(bool isOn);

        void DisableAllDigitalOutputs();
    }
}
