namespace CpuEmulator
{
    public struct Data12Bit
    {
        public Data12Bit(uint val)
        {
            _val = val;
        }
        public override string ToString()
        {
            return ValA.ToString();
        }
        private uint _val;
        public uint ValA
        {
            get => _val;
            set => _val = value & 0xfff;
        }
        public uint ValB
        {
            get => _val;
            set => _val = value;
        }
    }
}