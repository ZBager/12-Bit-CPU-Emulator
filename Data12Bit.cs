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
			return Val.ToString();
		}
		private uint _val;
		public uint Val
		{
			get => _val;
			set => _val = value & 0xfff;
		}
	}
}