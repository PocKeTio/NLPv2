# NLP Classification System v2.0

A sophisticated text classification system that combines statistical and machine learning approaches to classify SWIFT messages in multiple languages.

## Features

- **Dual Classification Approach**
  - Statistical analysis based on weighted keywords
  - Machine Learning using ML.NET's multi-class classification
  - Weighted combination of both approaches for optimal results

- **Language Detection**
  - Automatic detection of English and French
  - Extensible to support additional languages
  - Language-aware classification

- **Modular Architecture**
  - Clean separation of concerns
  - Easy to extend and maintain
  - Well-documented code

## Requirements

- .NET Framework 4.8
- Microsoft Access Database (with SWIFT messages)
- ML.NET NuGet package
- Microsoft.ACE.OLEDB.12.0 provider

## Installation

1. Clone the repository
2. Open the solution in Visual Studio
3. Restore NuGet packages
4. Build the solution

## Database Structure

The Access database should contain a table with the following columns:
- `SWIFT` (text): The SWIFT message content
- `Category` (int): The classification category (0, 1, 2, etc.)
- `Language` (int): The language code (1=English, 2=French, 0=Unknown)

## Usage

1. Run the program
2. Enter the path to your Access database
3. The system will:
   - Load and train the ML model
   - Start an interactive console
   - Allow you to enter SWIFT messages for classification
   - Display detailed classification results

## Classification Categories

Current implementation supports three categories:
- 0: ExtendOrPay
- 1: Pricing
- 2: Termination

You can extend these categories by modifying the `_keywordsByCategory` dictionary in the `StatisticalApproach` class.

## Improvement Areas

1. **Language Detection**
   - Integrate with professional language detection libraries
   - Add support for more languages
   - Improve keyword dictionaries

2. **Statistical Approach**
   - Add phrase detection
   - Implement negation handling
   - Consider word order and proximity

3. **Machine Learning**
   - Experiment with different ML.NET algorithms
   - Add cross-validation
   - Implement model persistence

4. **Architecture**
   - Add logging
   - Implement dependency injection
   - Create REST API wrapper

## License

MIT License

## Contributing

Feel free to submit issues and enhancement requests!

## Author

Created by [Your Name]
