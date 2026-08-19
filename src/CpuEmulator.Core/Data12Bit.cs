namespace CpuEmulator
{
	public struct Data12Bit
	{
		public Data12Bit(uint val)
		{
			//Go through the setter so the 12-bit mask lives in exactly one place.
			//Assigning _val directly here used to let the constructor produce
			//values wider than 12 bits, breaking the type's whole invariant.
			_val = 0;
			Val = val;
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