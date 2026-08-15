using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;

namespace DataStructures;

public sealed class Cohort
{
    private sealed class DataTransferObject
    {
        public List<string> PatientIds { get; set; } = new();
        public List<string> Outcomes { get; set; } = new();
    }

    private Cohort(ImmutableArray<string> patientIds, ImmutableArray<string> outcomes)
    {
        // Private constructor to make sure object creation goes through Create().
        PatientIds = patientIds;
        Outcomes = outcomes;
    }

    public ImmutableArray<string> PatientIds { get; }
    public ImmutableArray<string> Outcomes { get; }

    public static Result<Cohort> Create(IReadOnlyList<string> patientIds, IReadOnlyList<string> outcomes)
    {
        if (patientIds.Count == 0)
        {
            return new Failure<Cohort>("A cohort must have at least one patient.");
        }

        if (outcomes.Count != patientIds.Count)
        {
            return new Failure<Cohort>(
                $"There are {outcomes.Count} outcome value(s) but {patientIds.Count} patient ID(s).");
        }

        var seenPatientIds = new HashSet<string>();
        for (var i = 0; i < patientIds.Count; i++)
        {
            var patientId = patientIds[i];
            if (string.IsNullOrWhiteSpace(patientId))
            {
                return new Failure<Cohort>($"Patient ID at row {i + 1} is missing.");
            }

            if (!seenPatientIds.Add(patientId))
            {
                return new Failure<Cohort>($"Patient ID '{patientId}' appears more than once.");
            }
        }

        for (var i = 0; i < outcomes.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(outcomes[i]))
            {
                return new Failure<Cohort>($"Outcome value for patient '{patientIds[i]}' is missing.");
            }
        }

        return new Success<Cohort>(new Cohort(patientIds.ToImmutableArray(), outcomes.ToImmutableArray()));
    }

    public List<byte> ToByteArray()
    {
        var dto = new DataTransferObject
        {
            PatientIds = PatientIds.ToList(),
            Outcomes = Outcomes.ToList()
        };

        return JsonSerializer.SerializeToUtf8Bytes(dto).ToList();
    }

    public static Result<Cohort> FromByteArray(List<byte> bytes)
    {
        DataTransferObject? dto;
        try
        {
            dto = JsonSerializer.Deserialize<DataTransferObject>(bytes.ToArray());
        }
        catch (JsonException exception)
        {
            return new Failure<Cohort>($"Could not deserialize cohort data: {exception.Message}");
        }

        if (dto is null)
        {
            return new Failure<Cohort>("Could not deserialize cohort data: content was empty.");
        }

        return Create(dto.PatientIds, dto.Outcomes);
    }
}
