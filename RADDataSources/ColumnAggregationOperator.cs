namespace RADDataSources
{
	using System;
	using Skyline.DataMiner.Analytics.GenericInterface;

	public delegate object AggregationFunc(object a, object b);

	public enum ColumnAggregationOperation
	{
		Sum,
		Product,
		Min,
		Max,
		Average,
	}

	[GQIMetaData(Name = "Aggregate two columns")]
	public class ColumnAggregationOperator : IGQIRowOperator, IGQIInputArguments, IGQIColumnOperator
	{
		private readonly GQIColumnDropdownArgument _firstColumnArg = new GQIColumnDropdownArgument("First column")
		{
			IsRequired = true,
			Types = new[] { GQIColumnType.Int, GQIColumnType.Double, GQIColumnType.DateTime, GQIColumnType.TimeSpan },
		};

		private readonly GQIColumnDropdownArgument _secondColumnArg = new GQIColumnDropdownArgument("Second column")
		{
			IsRequired = true,
			Types = new[] { GQIColumnType.Int, GQIColumnType.Double, GQIColumnType.DateTime, GQIColumnType.TimeSpan },
		};

		// Using a plain string argument (validated manually) to avoid dependency on a dropdown argument type that might not exist in the environment.
		private readonly GQIStringDropdownArgument _operationArg = new GQIStringDropdownArgument("Operation", GetAggregationOptions())
		{
			IsRequired = true,
		};

		private readonly GQIStringArgument _outputNameArg = new GQIStringArgument("Output column name")
		{
			IsRequired = true,
		};

		private GQIColumn _firstColumn;
		private GQIColumn _secondColumn;
		private AggregationFunc _aggregationFunc;
		private GQIColumn _outputColumn;

		public GQIArgument[] GetInputArguments()
		{
			return new GQIArgument[] { _firstColumnArg, _secondColumnArg, _operationArg, _outputNameArg };
		}

		public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
		{
			if (!args.TryGetArgumentValue(_firstColumnArg, out _firstColumn) || _firstColumn == null)
				throw new ArgumentException("First column not provided");

			if (!args.TryGetArgumentValue(_secondColumnArg, out _secondColumn) || _secondColumn == null)
				throw new ArgumentException("Second column not provided");

			if (!args.TryGetArgumentValue(_operationArg, out string operationStr) || !Enum.TryParse(operationStr, true, out ColumnAggregationOperation operation))
				throw new ArgumentException("Operation not provided or invalid");

			if (!args.TryGetArgumentValue(_outputNameArg, out string outputName) || string.IsNullOrWhiteSpace(outputName))
				throw new ArgumentException("Output column name not provided");

			(_outputColumn, _aggregationFunc) = GetOutputColumnAndAggregationFunc(_firstColumn.Type, _secondColumn.Type, operation, outputName);

			return default;
		}

		public void HandleColumns(GQIEditableHeader header)
		{
			header.AddColumns(_outputColumn);
		}

		public void HandleRow(GQIEditableRow row)
		{
			if (row == null)
				return;

			object v1 = row.GetValue(_firstColumn.Name);
			object v2 = row.GetValue(_secondColumn.Name);

			row.SetValue(_outputColumn.Name, _aggregationFunc(v1, v2));
		}

		private static string[] GetAggregationOptions()
		{
			var ops = Enum.GetNames(typeof(ColumnAggregationOperation));
			for (int i = 0; i < ops.Length; i++)
			{
				ops[i] = ops[i];
			}

			return ops;
		}

		private static Tuple<GQIColumn, AggregationFunc> GetOutputColumnAndAggregationFunc(GQIColumnType firstType, GQIColumnType secondType,
			ColumnAggregationOperation operation, string columnName)
		{
			bool firstNumeric = firstType == GQIColumnType.Int || firstType == GQIColumnType.Double;
			bool secondNumeric = secondType == GQIColumnType.Int || secondType == GQIColumnType.Double;

			switch (operation)
			{
				case ColumnAggregationOperation.Sum:
					if (firstType == GQIColumnType.DateTime || secondType == GQIColumnType.DateTime)
						throw new ArgumentException("Cannot sum DateTime columns");

					if (firstNumeric)
					{
						if (!secondNumeric)
							throw new ArgumentException("Can only sum a numeric column with another numeric column");

						if (firstType == GQIColumnType.Int && secondType == GQIColumnType.Int)
							return Tuple.Create<GQIColumn, AggregationFunc>(new GQIIntColumn(columnName), SumInts);
						else
							return Tuple.Create<GQIColumn, AggregationFunc>(new GQIDoubleColumn(columnName), SumNumerics);
					}
					else
					{
						// First is TimeSpan now
						if (secondType != GQIColumnType.TimeSpan)
							throw new ArgumentException("Can only sum one TimeSpan column with another TimeSpan column");

						return Tuple.Create<GQIColumn, AggregationFunc>(new GQITimeSpanColumn(columnName), SumTimeSpans);
					}

				case ColumnAggregationOperation.Product:
					if (firstType == GQIColumnType.DateTime || secondType == GQIColumnType.DateTime)
						throw new ArgumentException("Cannot multiply DateTime columns");

					if (firstNumeric && secondNumeric)
					{
						if (firstType == GQIColumnType.Int && secondType == GQIColumnType.Int)
							return Tuple.Create<GQIColumn, AggregationFunc>(new GQIIntColumn(columnName), ProductInts);
						else
							return Tuple.Create<GQIColumn, AggregationFunc>(new GQIDoubleColumn(columnName), ProductNumerics);
					}
					else
					{
						// At least one is TimeSpan
						if (firstType == GQIColumnType.TimeSpan)
						{
							if (secondType == GQIColumnType.TimeSpan)
								throw new ArgumentException("Cannot multiply two TimeSpan columns");

							return Tuple.Create<GQIColumn, AggregationFunc>(new GQITimeSpanColumn(columnName), ProductTimeSpanWithNumeric);
						}
						else
						{
							// Second is TimeSpan now
							return Tuple.Create<GQIColumn, AggregationFunc>(new GQITimeSpanColumn(columnName), ProductNumericWithTimeSpan);
						}
					}

				case ColumnAggregationOperation.Min:
				case ColumnAggregationOperation.Max:
					if (firstType == GQIColumnType.DateTime || secondType == GQIColumnType.DateTime)
					{
						 if (!(firstType == GQIColumnType.DateTime && secondType == GQIColumnType.DateTime))
							throw new ArgumentException("Can only find min or max between two DateTime columns");

						 if (operation == ColumnAggregationOperation.Min)
							 return Tuple.Create<GQIColumn, AggregationFunc>(new GQIDateTimeColumn(columnName), MinDateTimes);
						 else
						  	 return Tuple.Create<GQIColumn, AggregationFunc>(new GQIDateTimeColumn(columnName), MaxDateTimes);
					}
					else if (firstNumeric || secondNumeric)
					{
						if (!(firstNumeric && secondNumeric))
							throw new ArgumentException("Can only find min or max between two numeric columns");
						if (firstType == GQIColumnType.Int && secondType == GQIColumnType.Int)
						{
							if (operation == ColumnAggregationOperation.Min)
								return Tuple.Create<GQIColumn, AggregationFunc>(new GQIIntColumn(columnName), MinInts);
							else
								return Tuple.Create<GQIColumn, AggregationFunc>(new GQIIntColumn(columnName), MaxInts);
						}
						else
						{
							if (operation == ColumnAggregationOperation.Min)
								return Tuple.Create<GQIColumn, AggregationFunc>(new GQIDoubleColumn(columnName), MinNumerics);
							else
								return Tuple.Create<GQIColumn, AggregationFunc>(new GQIDoubleColumn(columnName), MaxNumerics);
						}
					}
					else
					{
						// Both are TimeSpan now
						if (operation == ColumnAggregationOperation.Min)
							return Tuple.Create<GQIColumn, AggregationFunc>(new GQITimeSpanColumn(columnName), MinTimeSpans);
						else
							return Tuple.Create<GQIColumn, AggregationFunc>(new GQITimeSpanColumn(columnName), MaxTimeSpans);
					}

				case ColumnAggregationOperation.Average:
					if (firstType == GQIColumnType.DateTime || secondType == GQIColumnType.DateTime)
					{
						if (!(firstType == GQIColumnType.DateTime && secondType == GQIColumnType.DateTime))
							throw new ArgumentException("Can only find average between two DateTime columns");

						return Tuple.Create<GQIColumn, AggregationFunc>(new GQIDateTimeColumn(columnName), AverageDateTimes);
					}

					if (firstNumeric)
					{
						if (!secondNumeric)
							throw new ArgumentException("Can only average a numeric column with another numeric column");

						return Tuple.Create<GQIColumn, AggregationFunc>(new GQIDoubleColumn(columnName), AverageNumerics);
					}
					else
					{
						// First is TimeSpan now
						if (secondType != GQIColumnType.TimeSpan)
							throw new ArgumentException("Can only average one TimeSpan column with another TimeSpan column");

						return Tuple.Create<GQIColumn, AggregationFunc>(new GQITimeSpanColumn(columnName), AverageTimeSpans);
					}

				default:
					throw new ArgumentException($"Unsupported operation '{operation}'");
			}
		}

		private static bool TryConvertToDouble(object o, out double value)
		{
			if (o is double d)
			{
				value = d;
				return true;
			}

			if (o is int i)
			{
				value = i;
				return true;
			}

			value = 0;
			return false;
		}

		private static object SumInts(object a, object b)
		{
			if (a is int i1 && b is int i2)
				return i1 + i2;

			return null;
		}

		private static double InnerSumNumerics(object a, object b)
		{
			double d1, d2;
			if (!TryConvertToDouble(a, out d1) || !TryConvertToDouble(b, out d2))
				return 0.0;

			return d1 + d2;
		}

		private static object SumNumerics(object a, object b)
		{
			return InnerSumNumerics(a, b);
		}

		private static object SumTimeSpans(object a, object b)
		{
			if (a is TimeSpan ts1 && b is TimeSpan ts2)
				return ts1 + ts2;
			return null;
		}

		private static object ProductInts(object a, object b)
		{
			if (a is int i1 && b is int i2)
				return i1 * i2;

			return null;
		}

		private static object ProductNumerics(object a, object b)
		{
			double d1, d2;
			if (!TryConvertToDouble(a, out d1) || !TryConvertToDouble(b, out d2))
				return null;

			return d1 * d2;
		}

		private static object ProductTimeSpanWithNumeric(object a, object b)
		{
			if (a is TimeSpan ts && TryConvertToDouble(b, out double d))
				return TimeSpan.FromMinutes(ts.TotalMinutes * d);

			return null;
		}

		private static object ProductNumericWithTimeSpan(object a, object b)
		{
			if (b is TimeSpan ts && TryConvertToDouble(a, out double d))
				return TimeSpan.FromMinutes(ts.TotalMinutes * d);

			return null;
		}

		private static object MinInts(object a, object b)
		{
			if (a is int i1 && b is int i2)
				return Math.Min(i1, i2);

			return null;
		}

		private static object MinNumerics(object a, object b)
		{
			double d1, d2;
			if (TryConvertToDouble(a, out d1) && TryConvertToDouble(b, out d2))
				return Math.Max(d1, d2);

			return null;
		}

		private static object MinTimeSpans(object a, object b)
		{
			if (a is TimeSpan ts1 && b is TimeSpan ts2)
				return ts1 < ts2 ? ts1 : ts2;

			return null;
		}

		private static object MinDateTimes(object a, object b)
		{
			if (a is DateTime dt1 && b is DateTime dt2)
				return dt1 < dt2 ? dt1 : dt2;

			return null;
		}

		private static object MaxInts(object a, object b)
		{
			if (a is int i1 && b is int i2)
				return Math.Max(i1, i2);
			return null;
		}

		private static object MaxNumerics(object a, object b)
		{
			double d1, d2;
			if (TryConvertToDouble(a, out d1) && TryConvertToDouble(b, out d2))
				return Math.Max(d1, d2);

			return null;
		}

		private static object MaxTimeSpans(object a, object b)
		{
			if (a is TimeSpan ts1 && b is TimeSpan ts2)
				return ts1 > ts2 ? ts1 : ts2;

			return null;
		}

		private static object MaxDateTimes(object a, object b)
		{
			if (a is DateTime dt1 && b is DateTime dt2)
				return dt1 > dt2 ? dt1 : dt2;

			return null;
		}

		private static object AverageNumerics(object a, object b)
		{
			return InnerSumNumerics(a, b) / 2.0;
		}

		private static object AverageTimeSpans(object a, object b)
		{
			if (a is TimeSpan ts1 && b is TimeSpan ts2)
				return TimeSpan.FromMinutes((ts1.TotalMinutes + ts2.TotalMinutes) / 2.0);

			return null;
		}

		private static object AverageDateTimes(object a, object b)
		{
			if (a is DateTime dt1 && b is DateTime dt2)
			{
				long avgTicks = (dt1.Ticks + dt2.Ticks) / 2;
				return new DateTime(avgTicks);
			}

			return null;
		}
	}
}
