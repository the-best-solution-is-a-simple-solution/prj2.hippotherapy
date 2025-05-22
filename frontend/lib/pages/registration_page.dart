import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:form_builder_validators/form_builder_validators.dart';
import 'package:frontend/controllers/auth_controller.dart';
import 'package:frontend/models/therapist.dart';
import 'package:frontend/pages/login_page.dart';
import 'package:frontend/widgets/helper_widgets/alert_dialog_box.dart';
import 'package:frontend/widgets/navigation_widgets/drawer_widget.dart';

/// Page to handle Therapist registration for a new account
/// Uses firebase authentication to register users and stores various
/// supplemental information in the firebase emulator.
/// Uses https://pub.dev/packages/form_builder_validators for validation
class RegistrationPage extends StatefulWidget {
  static const String RouteName = '/register';
  final String code;
  final String email;
  final String ownerId;
  const RegistrationPage(
      {super.key, this.code = "", this.email = "", this.ownerId = ""});

  @override
  _RegistrationPageState createState() => _RegistrationPageState();
}

class _RegistrationPageState extends State<RegistrationPage> {
  final _formKey = GlobalKey<FormState>();
  final Map<String, dynamic> _formData = {};
  bool _isLoading = false;
  late String code;
  late String email;
  late String ownerId;

  @override
  void initState() {
    super.initState();

    // grab the parameters from the referral link if possible
    code = Uri.base.queryParameters['code'] ?? widget.code;
    email = Uri.base.queryParameters['email'] ?? widget.email;
    ownerId = Uri.base.queryParameters['owner'] ?? widget.ownerId;
  }

  // Register method to handle form information and pass it to Firebase
  Future<void> _register() async {
    if (!(_formKey.currentState?.validate() ?? false)) {
      return;
    }

    _formKey.currentState?.save();
    setState(() => _isLoading = true);

    try {
      final therapist = Therapist.fromJson(_formData);
      therapist.ownerId = ownerId; // assign therapist a owner id

      // verify therapist whether or not they provided the same email as the one provided in the owner interface.
      therapist.email == email
          ? therapist.verified = true
          : therapist.verified = false;

      final errorMessage = await AuthController()
          .registerTherapist(therapist, _formData['password']);

      if (errorMessage == "202") {
        return alertDialogBox(
          context,
          "Registration successful.",
          'Different email provided compared to owner referral. Check your email to verify it then you can log in.\n'
              'Verification may take a few minutes to send.\n'
              'You may need to check spam email',
          [
            TextButton(
              key: const Key('successDialogOkButton'),
              child: const Text('OK'),
              onPressed: () {
                Navigator.of(context).pop();
                Navigator.pushReplacementNamed(context, '/login');
              },
            ),
          ],
        );
      } else if (errorMessage == "406") {
        return alertDialogBox(context, "Invalid Referral Code",
            'Referral code has expired or is invalid. Please try again later or ask owner for another code.');
      } else if (errorMessage == null) {
        return alertDialogBox(
          context,
          "Registration successful.",
          'Please sign in.',
          [
            TextButton(
              key: const Key('successDialogOkButton'),
              child: const Text('OK'),
              onPressed: () {
                Navigator.of(context).pop();
                Navigator.pushReplacementNamed(context, LoginPage.RouteName);
              },
            ),
          ],
        );
      } else {
        _showSnackBar(errorMessage);
      }
    } catch (e) {
      _showSnackBar('Unexpected error: $e');
    } finally {
      setState(() => _isLoading = false);
    }
  }

  void _showSnackBar(final String message) {
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(
        key: const Key('registration_snackbar'),
        content: Text(message),
        duration: const Duration(seconds: 5)));
  }

  @override
  Widget build(final BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Register Therapist'),
        backgroundColor: Theme.of(context).colorScheme.primary,
        leading: Builder(
          key: const Key('hippotherapy_hamburger_menu'),
          builder: (final BuildContext context) {
            return IconButton(
              icon: const Icon(Icons.menu),
              onPressed: () {
                Scaffold.of(context).openDrawer();
              },
              tooltip: 'Menu',
            );
          },
        ),
      ),
      drawer: const HippoAppDrawer(),
      body: Center(
        child: SingleChildScrollView(
          child: Container(
            padding: const EdgeInsets.all(16.0),
            constraints: const BoxConstraints(maxWidth: 400),
            child: Form(
              key: _formKey,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  _buildFormField(
                      // REFERRAL CODE INPUT
                      key: const ValueKey("referralField"),
                      name: "referral",
                      label: 'Referral (Required)',
                      initialValue: code,
                      inputFormatters: [
                        LengthLimitingTextInputFormatter(6),
                        FilteringTextInputFormatter.allow(RegExp(r'[0-9]')),
                      ],
                      validator: FormBuilderValidators.compose([
                        FormBuilderValidators.required(
                          errorText: 'Referral code is required',
                        ),
                        FormBuilderValidators.minLength(6,
                            errorText: "Referral code must be 6 integers"),
                        FormBuilderValidators.maxLength(6,
                            errorText: "Referral code must be 6 integers"),
                      ]),
                      keyboardType: TextInputType.number),
                  // Email Field
                  _buildFormField(
                    key: const ValueKey('emailField'),
                    name: 'email',
                    label: 'Email (Required)',
                    initialValue: email,
                    inputFormatters: [
                      LengthLimitingTextInputFormatter(30),
                    ],
                    validator: FormBuilderValidators.compose([
                      FormBuilderValidators.required(
                          errorText: 'Email is required'),
                      FormBuilderValidators.maxLength(30,
                          errorText: 'Email must be at most 30 characters'),
                      FormBuilderValidators.email(
                          errorText: 'Please enter a valid email'),
                    ]),
                    keyboardType: TextInputType.emailAddress,
                  ),
                  // Password Field
                  _buildFormField(
                    key: const ValueKey('passwordField'),
                    name: 'password',
                    label: 'Password (Required)',
                    obscureText: true,
                    inputFormatters: [
                      LengthLimitingTextInputFormatter(20),
                    ],
                    validator: FormBuilderValidators.compose([
                      FormBuilderValidators.required(
                          errorText: 'Password is required'),
                      FormBuilderValidators.minLength(6,
                          errorText: 'Password must be at least 6 characters'),
                      FormBuilderValidators.maxLength(20,
                          errorText: 'Password must be at most 20 characters'),
                      FormBuilderValidators.hasSpecialChars(
                          atLeast: 1,
                          errorText:
                              'Password must contain at least one special character'),
                      FormBuilderValidators.hasNumericChars(
                          atLeast: 1,
                          errorText:
                              'Password must contain at least one number'),
                      FormBuilderValidators.hasUppercaseChars(
                          atLeast: 1,
                          errorText:
                              'Password must contain at least one uppercase character'),
                      FormBuilderValidators.hasLowercaseChars(
                          atLeast: 1,
                          errorText:
                              'Password must contain at least one lowercase character'),
                    ]),
                  ),
                  // First Name Field
                  _buildFormField(
                    key: const ValueKey('fNameField'),
                    name: 'fName',
                    label: 'First Name (Required)',
                    inputFormatters: [
                      LengthLimitingTextInputFormatter(20),
                    ],
                    validator: FormBuilderValidators.compose([
                      FormBuilderValidators.required(
                          errorText: 'First name is required'),
                      FormBuilderValidators.maxLength(20,
                          errorText:
                              'First name must be at most 20 characters'),
                    ]),
                  ),
                  // Last Name Field
                  _buildFormField(
                    key: const ValueKey('lNameField'),
                    name: 'lName',
                    label: 'Last Name (Required)',
                    inputFormatters: [
                      LengthLimitingTextInputFormatter(20),
                    ],
                    validator: FormBuilderValidators.compose([
                      FormBuilderValidators.required(
                          errorText: 'Last name is required'),
                      FormBuilderValidators.maxLength(20,
                          errorText: 'Last name must be at most 20 characters'),
                    ]),
                  ),
                  // Country Field
                  _buildFormField(
                    key: const ValueKey('countryField'),
                    name: 'country',
                    label: 'Country',
                    inputFormatters: [
                      LengthLimitingTextInputFormatter(20),
                    ],
                    validator: FormBuilderValidators.compose([
                      FormBuilderValidators.match(
                        RegExp(r'^[a-zA-Z\s]+$'),
                        errorText:
                            'Country must contain only letters and spaces',
                        checkNullOrEmpty: false,
                      ),
                      FormBuilderValidators.maxLength(20,
                          errorText: 'Country must be at most 20 characters',
                          checkNullOrEmpty: false),
                    ]),
                  ),
                  // City Field
                  _buildFormField(
                    key: const ValueKey('cityField'),
                    name: 'city',
                    label: 'City',
                    inputFormatters: [
                      LengthLimitingTextInputFormatter(20),
                    ],
                    validator: FormBuilderValidators.compose([
                      FormBuilderValidators.match(
                        RegExp(r'^[a-zA-Z\s]+$'),
                        errorText: 'City must contain only letters and spaces',
                        checkNullOrEmpty: false,
                      ),
                      FormBuilderValidators.maxLength(20,
                          errorText: 'City must be at most 20 characters.',
                          checkNullOrEmpty: false),
                    ]),
                  ),
                  // Street Field
                  _buildFormField(
                    key: const ValueKey('streetField'),
                    name: 'street',
                    label: 'Street',
                    inputFormatters: [
                      LengthLimitingTextInputFormatter(20),
                    ],
                    validator: FormBuilderValidators.compose([
                      FormBuilderValidators.match(
                        RegExp(r"^[A-Za-zÀ-ÿ0-9\s.,\-'\/]+$"),
                        errorText:
                            'Street must contain only letters, numbers, spaces, and common punctuation.',
                        checkNullOrEmpty: false,
                      ),
                      FormBuilderValidators.maxLength(20,
                          errorText: 'Street must be at most 20 characters.',
                          checkNullOrEmpty: false),
                    ]),
                  ),
                  // Postal Code Field
                  _buildFormField(
                    key: const ValueKey('postalCodeField'),
                    name: 'postalCode',
                    label: 'Postal Code',
                    validator: FormBuilderValidators.match(
                      RegExp(r'^[A-Za-z]\d[A-Za-z][ -]?\d[A-Za-z]\d$'),
                      errorText: 'Postal code should be in the form L#L #L#.',
                      checkNullOrEmpty: false,
                    ),
                  ),
                  // Phone Field
                  _buildFormField(
                    key: const ValueKey('phoneField'),
                    name: 'phone',
                    label: 'Phone',
                    validator: FormBuilderValidators.compose([
                      FormBuilderValidators.match(
                        RegExp(
                            r'^(\+\d{1,2}\s)?\(?\d{3}\)?[\s.-]\d{3}[\s.-]\d{4}$'),
                        errorText: 'Please enter a 10 digit phone number',
                        checkNullOrEmpty: false,
                      ),
                    ]),
                  ),
                  // Profession Field
                  _buildFormField(
                    key: const ValueKey('professionField'),
                    name: 'profession',
                    label: 'Profession',
                    inputFormatters: [
                      LengthLimitingTextInputFormatter(25),
                    ],
                    validator: FormBuilderValidators.compose([
                      FormBuilderValidators.match(
                        RegExp(r'^[a-zA-Z\s]+$'),
                        errorText:
                            'Profession must contain only letters and spaces',
                        checkNullOrEmpty: false,
                      ),
                      FormBuilderValidators.maxLength(25,
                          errorText:
                              'Profession must be at most 25 characters long',
                          checkNullOrEmpty: false),
                    ]),
                  ),
                  // Major Field
                  _buildFormField(
                    key: const ValueKey('majorField'),
                    name: 'major',
                    label: 'Major',
                    inputFormatters: [
                      LengthLimitingTextInputFormatter(25),
                    ],
                    validator: FormBuilderValidators.compose([
                      FormBuilderValidators.maxLength(25,
                          errorText: 'Major must be at most 25 characters long',
                          checkNullOrEmpty: false),
                      FormBuilderValidators.match(
                        RegExp(r'^[a-zA-Z\s]+$'),
                        errorText: 'Major must contain only letters and spaces',
                        checkNullOrEmpty: false,
                      ),
                    ]),
                  ),
                  // Years of Experience Field
                  _buildFormField(
                    key: const ValueKey('yearsExpInHippotherapyField'),
                    name: 'yearsExpInHippotherapy',
                    label: 'Years of Experience in Hippotherapy',
                    keyboardType: TextInputType.number,
                    validator: FormBuilderValidators.compose([
                      FormBuilderValidators.integer(
                          errorText: "Please enter an integer.",
                          checkNullOrEmpty: false),
                      FormBuilderValidators.min(0,
                          errorText: "Please enter a positive integer.",
                          checkNullOrEmpty: false),
                      FormBuilderValidators.max(100,
                          errorText: "Please enter an integer below 100.",
                          checkNullOrEmpty: false)
                    ]),
                  ),
                  // Hide register button when loading.
                  if (_isLoading)
                    const Center(child: CircularProgressIndicator())
                  else
                    ElevatedButton(
                      key: const ValueKey('registerButton'),
                      onPressed: _register,
                      child: const Text('Register'),
                    ),
                  const SizedBox(height: 20),
                  // Register Button with Key

                  // Login link for users who are already registered
                  Align(
                    alignment: Alignment.center,
                    child: TextButton(
                      onPressed: () {
                        Navigator.pushReplacementNamed(
                            context, LoginPage.RouteName);
                      },
                      child: const Text(
                        'Already registered? Login here',
                        style: TextStyle(color: Colors.blue),
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }

  // Define a customer FormField
  Widget _buildFormField({
    required final String name,
    required final String label,
    final String? Function(String?)? validator,
    final bool obscureText = false,
    final List<TextInputFormatter>? inputFormatters,
    final TextInputType keyboardType = TextInputType.text,
    final Key? key,
    final String? initialValue,
  }) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 6.0), // reduce size to make tests more consistent
      child: TextFormField(
        key: key,
        initialValue: initialValue,
        decoration: InputDecoration(labelText: label),
        obscureText: obscureText,
        inputFormatters: inputFormatters,
        keyboardType: keyboardType,
        autovalidateMode: AutovalidateMode.onUserInteraction,
        validator: validator,
        onSaved: (final value) => _formData[name] = value?.trim(),
      ),
    );
  }
}
