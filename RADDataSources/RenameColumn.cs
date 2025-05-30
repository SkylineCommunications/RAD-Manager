namespace RadDataSources
{
	using Skyline.DataMiner.Analytics.GenericInterface;

	[GQIMetaData(Name = "Rename Column")]
	public class MyCustomOperator : IGQIColumnOperator, IGQIInputArguments
	{
		private GQIColumnDropdownArgument _columnArg = new GQIColumnDropdownArgument("Column") { IsRequired = true };
		private GQIStringArgument _nameArg = new GQIStringArgument("New name") { IsRequired = true };

		private GQIColumn _column;
		private string _newName;

		public GQIArgument[] GetInputArguments()
		{
			return new GQIArgument[] { _columnArg, _nameArg };
		}

		public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
		{
			_column = args.GetArgumentValue(_columnArg);
			_newName = args.GetArgumentValue(_nameArg);

			return new OnArgumentsProcessedOutputArgs();
		}

		public void HandleColumns(GQIEditableHeader header)
		{
			header.RenameColumn(_column, _newName);
		}
	}
}