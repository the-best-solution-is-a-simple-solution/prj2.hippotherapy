import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:form_builder_validators/form_builder_validators.dart';
import 'package:frontend/controllers/auth_controller.dart';
import 'package:frontend/models/owner.dart';
import 'package:frontend/pages/login_page.dart';
import 'package:frontend/widgets/navigation_widgets/drawer_widget.dart';

class OwnerRegistrationPage extends StatefulWidget {
  static const String RouteName = '/owner-register';
  const OwnerRegistrationPage({super.key});

  @override
  _OwnerRegistrationPageState createState() => _OwnerRegistrationPageState();
}

class _OwnerRegistrationPageState extends State<OwnerRegistrationPage> {
  final _formKey = GlobalKey<FormState>();
  final Map<String, dynamic> _formData = {};
  bool _isLoading = false;

  // Register method to handle form information and pass it to Firebase
  Future<void> _register() async {
    if (!(_formKey.currentState?.validate() ?? false)) {
      return;
    }
    debugPrint('In register method registration page');
    _formKey.currentState?.save();
    setState(() => _isLoading = true);

    try {
      final owner = Owner.fromJson(_formData);
      final errorMessage =
          await AuthController().registerOwner(owner, _formData['password']);

      if (mounted && errorMessage == null) {
        Navigator.pushReplacementNamed(context, LoginPage.RouteName);
        _showSuccessDialog(
            'Registration successful. Check your email to verify it then you can log in.\n'
            'Verification may take a few minutes to send.\n'
            'You may need to check spam email.');
      } else {
        _showSnackBar(errorMessage ?? 'Error was null');
      }
    } catch (e) {
      _showSnackBar('Unexpected error: $e');
    } finally {
      setState(() => _isLoading = false);
    }
  }

  void _showSnackBar(final String message) {
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(
        key: const Key('owner_snackbar'),
        content: Text(message),
        duration: const Duration(seconds: 5)));
  }

  Future<void> _showSuccessDialog(final String message) async {
    return showDialog<void>(
      context: context,
      barrierDismissible: false,
      builder: (final BuildContext context) {
        return AlertDialog(
          key: const Key('owner_success_dialog'),
          title: Text(message),
          content: const SingleChildScrollView(
            child: ListBody(
              children: <Widget>[
                Text(''),
              ],
            ),
          ),
          actions: <Widget>[
            TextButton(
              key: const Key('ownerSuccessDialogOkButton'),
              child: const Text('OK'),
              onPressed: () {
                Navigator.of(context).pop();
              },
            ),
          ],
        );
      },
    );
  }

  Widget _buildFormField({
    required final String name,
    required final String label,
    final String? Function(String?)? validator,
    final bool obscureText = false,
    final List<TextInputFormatter>? inputFormatters,
    final TextInputType keyboardType = TextInputType.text,
    final Key? key,
  }) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 16.0),
      child: TextFormField(
        key: key,
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

  @override
  Widget build(final BuildContext context) {
    return Scaffold(
        appBar: AppBar(
          title: const Text('Register Owner'),
          backgroundColor: Theme.of(context).colorScheme.primary,
          leading: Builder(
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
                    // Email Field
                    _buildFormField(
                      key: const Key('owner_register_email'),
                      name: 'email',
                      label: 'Email (Required)',
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
                      key: const Key('owner_register_pass'),
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
                            errorText:
                                'Password must be at least 6 characters'),
                        FormBuilderValidators.maxLength(20,
                            errorText:
                                'Password must be at most 20 characters'),
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
                      key: const Key('owner_register_fname'),
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
                      key: const Key('owner_register_lname'),
                      name: 'lName',
                      label: 'Last Name (Required)',
                      inputFormatters: [
                        LengthLimitingTextInputFormatter(20),
                      ],
                      validator: FormBuilderValidators.compose([
                        FormBuilderValidators.required(
                            errorText: 'Last name is required'),
                        FormBuilderValidators.maxLength(20,
                            errorText:
                                'Last name must be at most 20 characters'),
                      ]),
                    ),
                    // Hide register button when loading.
                    if (_isLoading)
                      const Center(child: CircularProgressIndicator())
                    else
                      ElevatedButton(
                        key: const ValueKey('owner_register_button'),
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
        ));
  }
}
