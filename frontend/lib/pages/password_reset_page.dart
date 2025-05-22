import 'dart:io';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_form_builder/flutter_form_builder.dart';
import 'package:form_builder_validators/form_builder_validators.dart';
import 'package:frontend/main.dart';
import 'package:http/http.dart';

class PasswordResetPage extends StatefulWidget {
  static const String RouteName = '/reset-password';
  final Uri? testPasswdResetLink;

  const PasswordResetPage({super.key, this.testPasswdResetLink});

  @override
  PasswordResetPageState createState() => PasswordResetPageState();
}

class PasswordResetPageState extends State<PasswordResetPage> {
  final GlobalKey<FormBuilderState> formKey = GlobalKey<FormBuilderState>();

  String? errorMessage;
  Uri? passwdResetLink;
  bool pwdVis = false;
  bool pwdConfVis = false;

  @override
  void initState() {
    super.initState();
    // Extract the link from the email, using a test specific link if passed in
    passwdResetLink = widget.testPasswdResetLink ??
        Uri.parse(Uri.base.queryParameters['link'] ?? "");
  }

  Future<void> resetPassword() async {
    if (formKey.currentState?.saveAndValidate() ?? false) {
      if (passwdResetLink == null || passwdResetLink.toString() == '') {
        setState(() {
          errorMessage = "Invalid password reset link.";
        });
        return;
      }

      try {
        // Extract the new password from the form
        final newPwd = formKey.currentState!.value['conf_pwd_field'];

        // Create a map to hold the query parameters
        final mapParams = <String, String>{};

        // Copy existing query parameters from the reset link
        passwdResetLink!.queryParameters.forEach((final k, final v) {
          mapParams[k] = v;
        });

        // Add the new password, ensuring it is URL-encoded
        mapParams['newPassword'] = newPwd;

        // Construct the new URI with the updated query parameters
        final Uri link = Uri(
          scheme: passwdResetLink!.scheme,
          host: passwdResetLink!.host,
          port: passwdResetLink!.port,
          path: passwdResetLink!.path,
          queryParameters: mapParams,
        );

        // Make a GET request to the reset link
        final res = await get(link);

        if (res.statusCode != HttpStatus.ok) {
          throw Exception(res.body);
        }

        // Show success message
        if (mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text("Password reset successfully!")),
          );
          Navigator.pushReplacementNamed(context, '/login');
        }
      } catch (e) {
        debugPrint(e.toString());
        setState(() {
          errorMessage = "Failed to reset password. Please try again.";
        });
      }
    }
  }

  Widget submitNewPassButton() {
    return ElevatedButton(
      key: const Key('nu_pwd_submit'),
      onPressed: resetPassword,
      child: const Text("OK"),
    );
  }

  Widget pwdField(final String name, final String label, final bool visToggle,
      final VoidCallback onVisBtnPress,
      {final String? matchField}) {
    return FormBuilderTextField(
      key: Key(name),
      name: name,
      obscureText: !visToggle,
      // this must be reversed
      decoration: InputDecoration(
        labelText: label,
        border: const OutlineInputBorder(),
        suffixIcon: IconButton(
          icon: Icon(
            visToggle ? Icons.visibility : Icons.visibility_off,
            semanticLabel: visToggle ? 'Hide password' : 'Show password',
          ),
          onPressed: onVisBtnPress,
        ),
      ),
      inputFormatters: [
        LengthLimitingTextInputFormatter(20),
      ],
      validator: FormBuilderValidators.compose([
        FormBuilderValidators.required(errorText: 'Password is required'),
        FormBuilderValidators.minLength(6,
            errorText: 'Password must be at least 6 characters'),
        FormBuilderValidators.maxLength(20,
            errorText: 'Password must be at most 20 characters'),
        FormBuilderValidators.hasSpecialChars(
            atLeast: 1,
            errorText: 'Password must contain at least one special character'),
        FormBuilderValidators.hasNumericChars(
            atLeast: 1, errorText: 'Password must contain at least one number'),
        FormBuilderValidators.hasUppercaseChars(
            atLeast: 1,
            errorText:
                'Password must contain at least one uppercase character'),
        FormBuilderValidators.hasLowercaseChars(
            atLeast: 1,
            errorText:
                'Password must contain at least one lowercase character'),
        if (matchField != null)
          (final value) {
            final newPassword = formKey.currentState?.fields[matchField]?.value;
            if (value != newPassword) {
              return 'Password fields must match match';
            }
            return null;
          },
      ]),
    );
  }

  Widget displayPasswordFields() {
    return FormBuilder(
      key: formKey,
      child: Column(
        children: [
          // Top field
          pwdField('pwd_field', 'New Password', pwdVis, () {
            setState(() {
              pwdVis = !pwdVis;
            });
          }),
          const SizedBox(height: 16),
          // bottom field match-bound to top
          pwdField('conf_pwd_field', 'Confirm Password', pwdConfVis, () {
            setState(() {
              pwdConfVis = !pwdConfVis;
            });
          }, matchField: 'pwd_field'),
          if (errorMessage != null)
            Padding(
              padding: const EdgeInsets.only(top: 16),
              child: Text(
                '${errorMessage!}\nPlease request another email or try again later.',
                style: const TextStyle(color: Colors.red),
              ),
            ),
        ],
      ),
    );
  }

  @override
  Widget build(final BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text("Password Reset Page"),
        backgroundColor: Theme.of(context).colorScheme.primary,
      ),
      body: Center(
        child: Container(
          constraints: const BoxConstraints(maxWidth: 400),
          padding: const EdgeInsets.all(16),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              if (passwdResetLink == null)
                const Text("Invalid password reset link.")
              else
                displayPasswordFields(),
              const SizedBox(height: 16),
              submitNewPassButton(),
            ],
          ),
        ),
      ),
    );
  }
}
