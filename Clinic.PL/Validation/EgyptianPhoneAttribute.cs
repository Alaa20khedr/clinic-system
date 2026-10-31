using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Clinic.BLL.Helpers;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Clinic.PL.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public class EgyptianPhoneAttribute : ValidationAttribute, IClientModelValidator
{
    public EgyptianPhoneAttribute()
        : base("Enter a valid Egyptian mobile number (e.g. 01012345678).")
    {
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
        {
            return ValidationResult.Success;
        }

        var strValue = value.ToString();
        if (string.IsNullOrWhiteSpace(strValue))
        {
            return ValidationResult.Success;
        }

        if (PhoneNumberNormalizer.TryNormalize(strValue, out _))
        {
            return ValidationResult.Success;
        }

        return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
    }

    public void AddValidation(ClientModelValidationContext context)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));

        MergeAttribute(context.Attributes, "data-val", "true");
        MergeAttribute(context.Attributes, "data-val-egyptianphone", FormatErrorMessage(context.ModelMetadata.GetDisplayName()));
    }

    private static void MergeAttribute(IDictionary<string, string> attributes, string key, string value)
    {
        if (!attributes.ContainsKey(key))
        {
            attributes.Add(key, value);
        }
    }
}

