﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using System.ComponentModel;
using System.Drawing.Design;

namespace Bonsai.OEPCIe
{
    using oe;

    [Description("Aquires data from a single TS4231 light to digital converter chip.")]
    public class LightHouseDevice : Source<LightHouseDataFrame>
    {
        private OEPCIeDisposable oepcie; // Reference to global oepcie configuration set
        private Dictionary<int, oe.lib.device_t> devices;
        IObservable<LightHouseDataFrame> source;
        private int hardware_clock_hz;

        public LightHouseDevice() {

            // Reference to context
            this.oepcie = OEPCIeManager.ReserveDAQ();

            // Find the hardware clock rate
            hardware_clock_hz = oepcie.DAQ.AcquisitionClockHz;

            // Find all RHD devices
            devices = oepcie.DAQ.DeviceMap.Where(
                    pair => pair.Value.id == (uint)Device.DeviceID.TS4231
            ).ToDictionary(x => x.Key, x => x.Value);

            // Stop here if there are no devices to use
            if (devices.Count == 0)
                throw new OEException((int)oe.lib.Error.DEVIDX);

            DeviceIndex = new DeviceIndexSelection();
            DeviceIndex.Indices = devices.Keys.ToArray();

            source = Observable.Create<LightHouseDataFrame>(observer =>
            {
                EventHandler<FrameReceivedEventArgs> inputReceived;
                var data_block = new LightHouseDataBlock(BlockSize);

                oepcie.Environment.Start();

                inputReceived = (sender, e) =>
                {
                    var frame = e.Value;

                    // If this frame contains data from the selected device_index
                    if (frame.DeviceIndices.Contains(DeviceIndex.SelectedIndex))
                    {

                        // Pull the sample
                        if (data_block.FillFromFrame(frame, DeviceIndex.SelectedIndex))
                        {
                            observer.OnNext(new LightHouseDataFrame(data_block, hardware_clock_hz));
                            data_block = new LightHouseDataBlock(BlockSize);
                        }
                    }
                };

                oepcie.Environment.FrameInputReceived += inputReceived;
                return Disposable.Create(() =>
                {
                    oepcie.Environment.FrameInputReceived -= inputReceived;
                    oepcie.Dispose();
                });
            });
        }

        public override IObservable<LightHouseDataFrame> Generate()
        {
            return source;
        }

        [Editor("Bonsai.OEPCIe.Design.DeviceIndexCollectionEditor, Bonsai.OEPCIe.Design", typeof(UITypeEditor))]
        [Description("The TS4231 optical to digital converter handled by this node.")]
        public DeviceIndexSelection DeviceIndex { get; set; }

        [Range(1, 100)]
        [Description("The size of data blocks, in samples, that are propogated in the observable sequence.")]
        public int BlockSize { get; set; } = 5;

        [Range(0, (int)1e9)]
        [Description("The remote clock frequency in Hz.")]
        public int RemoteClockHz { get; set; } = (int)42e6;
    }
}
