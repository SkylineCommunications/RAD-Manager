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

		private readonly GQIStringDropdownArgument _operationArg = new GQIStringDropdownArgument("Operation", Enum.GetNames(typeof(ColumnAggregationOperation)))
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

		private static Tuple<GQIColumn, AggregationFunc> GetSumOutputColumnAggregationFunc(GQIColumnType firstType, GQIColumnType secondType, string columnName)
		{
			switch (firstType)
			{
				case GQIColumnType.DateTime:
					throw new ArgumentException("Cannot sum DateTime columns");
				case GQIColumnType.Int:
				case GQIColumnType.Double:
					if (secondType != GQIColumnType.Int && secondType != GQIColumnType.Double)
						throw new ArgumentException("Can only sum a numeric column with another numeric column");

					if (firstType == GQIColumnType.Int && secondType == GQIColumnType.Int)
						return Tuple.Create<GQIColumn, AggregationFunc>(new GQIIntColumn(columnName), SumInts);
					else
						return Tuple.Create<GQIColumn, AggregationFunc>(new GQIDoubleColumn(columnName), SumNumerics);

				case GQIColumnType.TimeSpan:
					if (secondType != GQIColumnType.TimeSpan)
						throw new ArgumentException("Can only sum a TimeSpan column with another TimeSpan column");

					return Tuple.Create<GQIColumn, AggregationFunc>(new GQITimeSpanColumn(columnName), SumTimeSpans);

				default:
					throw new ArgumentException("Unsupported column type for sum operation");
			}
		}

		private static Tuple<GQIColumn, AggregationFunc> GetProductOutputColumnAggregationFunc(GQIColumnType firstType, GQIColumnType secondType, string columnName)
		{
			if (firstType == GQIColumnType.DateTime || secondType == GQIColumnType.DateTime)
				throw new ArgumentException("Cannot multiply DateTime columns");

			switch (firstType)
			{
				case GQIColumnType.Int:
				case GQIColumnType.Double:
					if (secondType == GQIColumnType.Int || secondType == GQIColumnType.Double)
					{
						if (firstType == GQIColumnType.Int && secondType == GQIColumnType.Int)
							return Tuple.Create<GQIColumn, AggregationFunc>(new GQIIntColumn(columnName), ProductInts);
						else
							return Tuple.Create<GQIColumn, AggregationFunc>(new GQIDoubleColumn(columnName), ProductNumerics);
					}
					else
					{
						// Second is TimeSpan
						return Tuple.Create<GQIColumn, AggregationFunc>(new GQITimeSpanColumn(columnName), ProductNumericWithTimeSpan);
					}

				case GQIColumnType.TimeSpan:
					if (secondType == GQIColumnType.TimeSpan)
						throw new ArgumentException("Cannot multiply two TimeSpan columns");
					else // Second is numeric
						return Tuple.Create<GQIColumn, AggregationFunc>(new GQITimeSpanColumn(columnName), ProductTimeSpanWithNumeric);
				default:
					throw new ArgumentException("Unsupported column type for product operation");
			}
		}

		private static Tuple<GQIColumn, AggregationFunc> GetMinMaxOutputColumnAggregationFunc(GQIColumnType firstType, GQIColumnType secondType, string columnName,
			AggregationFunc dateTimeAggregation, AggregationFunc intAggregation, AggregationFunc numericAggregation, AggregationFunc timeSpanAggegation)
		{
			switch (firstType)
			{
				case GQIColumnType.DateTime:
					if (secondType != GQIColumnType.DateTime)
						throw new ArgumentException("Can only find min or max between two DateTime columns");

					return Tuple.Create<GQIColumn, AggregationFunc>(new GQIDateTimeColumn(columnName), dateTimeAggregation);
				case GQIColumnType.Int:
				case GQIColumnType.Double:
					if (secondType != GQIColumnType.Int && secondType != GQIColumnType.Double)
						throw new ArgumentException("Can only find min or max between two numeric columns");

					if (firstType == GQIColumnType.Int && secondType == GQIColumnType.Int)
						return Tuple.Create<GQIColumn, AggregationFunc>(new GQIIntColumn(columnName), intAggregation);
					else
						return Tuple.Create<GQIColumn, AggregationFunc>(new GQIIntColumn(columnName), numericAggregation);
				case GQIColumnType.TimeSpan:
					if (secondType != GQIColumnType.TimeSpan)
						throw new ArgumentException("Can only find min or max between two TimeSpan columns");

					return Tuple.Create<GQIColumn, AggregationFunc>(new GQITimeSpanColumn(columnName), timeSpanAggegation);
				default:
					throw new ArgumentException("Unsupported column type for min or max operation");
			}
		}

		private static Tuple<GQIColumn, AggregationFunc> GetMinOutputColumnAggregationFunc(GQIColumnType firstType, GQIColumnType secondType, string columnName)
		{
			return GetMinMaxOutputColumnAggregationFunc(firstType, secondType, columnName, MinDateTimes, MinInts, MinNumerics, MinTimeSpans);
		}

		private static Tuple<GQIColumn, AggregationFunc> GetMaxOutputColumnAggregationFunc(GQIColumnType firstType, GQIColumnType secondType, string columnName)
		{
			return GetMinMaxOutputColumnAggregationFunc(firstType, secondType, columnName, MaxDateTimes, MaxInts, MaxNumerics, MaxTimeSpans);
		}

		private static Tuple<GQIColumn, AggregationFunc> GetAverageOutputColumnAggregationFunc(GQIColumnType firstType, GQIColumnType secondType, string columnName)
		{
			switch (firstType)
			{
				case GQIColumnType.DateTime:
					if (secondType != GQIColumnType.DateTime)
						throw new ArgumentException("Can only find average between two DateTime columns");

					return Tuple.Create<GQIColumn, AggregationFunc>(new GQIDateTimeColumn(columnName), AverageDateTimes);
				case GQIColumnType.Int:
				case GQIColumnType.Double:
					if (secondType != GQIColumnType.Int && secondType != GQIColumnType.Double)
						throw new ArgumentException("Can only average a numeric column with another numeric column");

					return Tuple.Create<GQIColumn, AggregationFunc>(new GQIDoubleColumn(columnName), AverageNumerics);
				case GQIColumnType.TimeSpan:
					if (secondType != GQIColumnType.TimeSpan)
						throw new ArgumentException("Can only average one TimeSpan column with another TimeSpan column");

					return Tuple.Create<GQIColumn, AggregationFunc>(new GQITimeSpanColumn(columnName), AverageTimeSpans);
				default:
					throw new ArgumentException("Unsupported column type for average operation");
			}
		}

		private static Tuple<GQIColumn, AggregationFunc> GetOutputColumnAndAggregationFunc(GQIColumnType firstType, GQIColumnType secondType,
			ColumnAggregationOperation operation, string columnName)
		{
			switch (operation)
			{
				case ColumnAggregationOperation.Sum:
					return GetSumOutputColumnAggregationFunc(firstType, secondType, columnName);
				case ColumnAggregationOperation.Product:
					return GetProductOutputColumnAggregationFunc(firstType, secondType, columnName);
				case ColumnAggregationOperation.Min:
					return GetMinOutputColumnAggregationFunc(firstType, secondType, columnName);
				case ColumnAggregationOperation.Max:
					return GetMaxOutputColumnAggregationFunc(firstType, secondType, columnName);
				case ColumnAggregationOperation.Average:
					return GetAverageOutputColumnAggregationFunc(firstType, secondType, columnName);
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

		private static object ProductTimeSpanWithNumeric(object timeSpan, object numeric)
		{
			if (timeSpan is TimeSpan ts && TryConvertToDouble(numeric, out double d))
				return TimeSpan.FromMinutes(ts.TotalMinutes * d);

			return null;
		}

		private static object ProductNumericWithTimeSpan(object numeric, object timeSpan)
		{
			return ProductTimeSpanWithNumeric(timeSpan, numeric);
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
				return Math.Min(d1, d2);

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
