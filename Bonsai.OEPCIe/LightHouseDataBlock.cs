﻿using System;
using System.Collections.Generic;

namespace Bonsai.OEPCIe
{
    public class LightHouseDataBlock
    {
        private int SamplesPerBlock;

        private int index = 0;
        Queue<ushort> raw_samples = new Queue<ushort>();
        ulong[] clock;
        ushort[] highLow;

        public LightHouseDataBlock(int samples_per_block = 5)
        {
            SamplesPerBlock = samples_per_block;

            AllocateArray1D(ref clock, samples_per_block);
            AllocateArray1D(ref highLow, samples_per_block);
        }

        public bool FillFromFrame(oe.Frame frame, int device_index)
        {
            if (index >= SamplesPerBlock)
                throw new IndexOutOfRangeException();

            // NB: Data contents: [uint16_t id, uint64_t remote_clock, uint16_t high_low]
            var sample = frame.Data<ushort>(device_index);

            clock[index] = ((ulong)sample[1] << 48) | ((ulong)sample[2] << 32) | ((ulong)sample[3] << 16) | ((ulong)sample[4] << 0);
            highLow[index] = sample[5];

            return ++index == SamplesPerBlock;
        }

        // Allocates memory for a 1-D array
        void AllocateArray1D<T>(ref T[] array1D, int xSize)
        {
            Array.Resize(ref array1D, xSize);
        }

        public ulong[] Clock
        {
            get { return clock; }
        }

        public ushort[] HighLow
        {
            get { return highLow; }
        }
    }
}
