import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:frontend/controllers/auth_controller.dart';
import 'package:frontend/models/role.dart';
import 'package:frontend/pages/owner_registration_page.dart';
import 'package:frontend/pages/patient_list_page.dart';
import 'package:frontend/pages/registration_page.dart';
import 'package:frontend/pages/therapist_list_page.dart';
import 'package:frontend/widgets/helper_widgets/alert_dialog_box.dart';
import 'package:frontend/widgets/navigation_widgets/drawer_widget.dart';
import 'package:http/http.dart' as http;
import 'package:provider/provider.dart';
import 'package:web/web.dart' as web;

class LoginPage extends StatefulWidget {
  static const String RouteName = '/login';

  const LoginPage({super.key, required this.title});

  final String title;

  @override
  LoginPageState createState() => LoginPageState();
}

class LoginPageState extends State<LoginPage> {
  late GlobalKey<FormState> formKey;
  final GlobalKey<ScaffoldState> scaffoldKey = GlobalKey<ScaffoldState>();
  final TextEditingController emailController = TextEditingController();
  final TextEditingController passwordController = TextEditingController();
  bool isLoading = false;
  bool passwordVisible = false;
  bool loginAsOwner = false;
  bool forgotPasswordPressed = false;
  bool isPasswordNotRequired = false;
  final Map<String, String> formData = {};

  @override
  void initState() {
    super.initState();
    checkLoginStatus();
    // parse the url and combine the individual params since the inner url has params
    if (Uri.base.queryParameters["verify"] != null) {
      verifyUser(Uri.parse(
          Uri.base.toString().split("verify=")[1].split("#")[0] ?? ""));
    }
  }

  // verifies the user by querying the param and making a get to the url provided
  Future<void> verifyUser(final Uri link) async {
    try {
      // if link actually exists
      if (link.toString().isNotEmpty) {
        final res = await http.get(link); // make get to it
        debugPrint(link.toString());
        if (res.statusCode == 200) {
          if (mounted) {
            alertDialogBox(
                context, "Email Verification Successful", "Please sign in.");
          }
        } else {
          if (mounted) {
            alertDialogBox(context, "Unable to verify email",
                "Status code: ${res.statusCode}");
          }
        }
      }
    } catch (e) {
      debugPrint("Unable to send email verification Error: ${e.toString()}");
    }

    // only works for web
    web.window.history.replaceState(null, '', '/'); // clears the url
  }

  Future<void> checkLoginStatus() async {
    final authController = Provider.of<AuthController>(context, listen: false);
    await authController.initialize();

    if (authController.isLoggedIn) {
      redirectLoggedInUser(authController);
    }
  }

  void redirectLoggedInUser(final AuthController authController) {
    WidgetsBinding.instance.addPostFrameCallback((final _) {
      if (mounted) {
        if (authController.therapistId != null) {
          Navigator.pushReplacementNamed(context, PatientList.RouteName);
        } else if (authController.ownerId != null) {
          Navigator.pushReplacementNamed(context, TherapistListPage.RouteName);
          Navigator.pushReplacementNamed(context, '/therapists');
        } else if (authController.isGuestLoggedIn()) {
          Navigator.pushReplacementNamed(context, '/patients');
        }
      }
    });
  }

  Widget forgotMyPasswordBtn() {
    return TextButton(
      key: const Key('forgot_pwd'),
      onPressed: () async {
        forgotPasswordPressed = true;

        if (formKey.currentState != null &&
            (!formKey.currentState!.validateGranularly().any(
                (final x) => x.widget.key == const Key('t_email_field')))) {
          bool wantToResetPassword = false;

          // show popup to prompt user for confirmation of email to send a password
          // reset to
          if (mounted) {
            wantToResetPassword = await showDialog<bool>(
                  context: context,
                  builder: (final BuildContext context) {
                    return AlertDialog(
                      title: Text('Is your email ${emailController.text}?'),
                      content: Text(
                        'We will send a password reset link to your email at ${emailController.text}\n'
                        'Is this correct?',
                      ),
                      actions: <Widget>[
                        TextButton(
                          onPressed: () {
                            // User cancels
                            Navigator.of(context).pop(false);
                          },
                          child: const Text('No'),
                        ),
                        TextButton(
                          key: const Key("req_pwd_reset"),
                          onPressed: () {
                            Navigator.of(context).pop(true); // user confirms
                          },
                          child: const Text('Yes'),
                        ),
                      ],
                    );
                  },
                ) ??
                false;
          }

          if (wantToResetPassword) {
            bool succeeded = false;
            String errorMsg = '';

            try {
              await AuthController()
                  .sendResetPasswordLink(emailController.text);
              succeeded = true;
            } catch (e) {
              errorMsg = e.toString().replaceAll('Exception: ', '');
              succeeded = false;
            } finally {
              mounted
                  ? await showDialog<bool>(
                      context: context,
                      builder: (final BuildContext context) {
                        return AlertDialog(
                          title: succeeded
                              ? const Text('Success')
                              : const Text('There was an error'),
                          content: succeeded
                              ? Text(
                                  'We have successfully sent an email to you at ${emailController.text}\n'
                                  'Please follow the instructions in the email.')
                              : Text(
                                  'We encountered an error trying to send you a password reset link email.\n'
                                  'Please try again later.\n'
                                  'The error reported was "$errorMsg"',
                                ),
                          actions: <Widget>[
                            TextButton(
                              key: const Key("req_pwd_reset_ack"),
                              onPressed: () {
                                Navigator.of(context).pop();
                              },
                              child: const Text('OK'),
                            ),
                          ],
                        );
                      },
                    )
                  : null;
            }
          }

          // push them back to the main page when we are done again to clear the fields
          // and reset the state
          mounted
              ? Navigator.pushReplacementNamed(context, LoginPage.RouteName)
              : null;
        }
      },
      child: const Text(
        'Forgot Password?',
        style: TextStyle(color: Colors.blue),
      ),
    );
  }

  @override
  Widget build(final BuildContext context) {
    final authController = Provider.of<AuthController>(context, listen: false);
    final screenWidth = MediaQuery.of(context).size.width;

    if (authController.isLoggedIn) {
      redirectLoggedInUser(authController);
    }

    return Scaffold(
      key: scaffoldKey,
      appBar: AppBar(
        backgroundColor: Theme.of(context).colorScheme.primary,
        title: Text(widget.title),
        leading: IconButton(
          key: const Key('hippotherapy_hamburger_menu'),
          icon: const Icon(Icons.menu),
          onPressed: () {
            scaffoldKey.currentState?.openDrawer();
          },
        ),
      ),
      drawer: const HippoAppDrawer(),
      body: SingleChildScrollView(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                crossAxisAlignment: CrossAxisAlignment.center,
                children: [
                  const SizedBox(height: 20),
                  Container(
                    padding: const EdgeInsets.all(16),
                    child: SizedBox(
                      height: MediaQuery.of(context).size.height * 0.4,
                      child: Image.asset(
                        'assets/images/logo.png',
                        fit: BoxFit.contain,
                      ),
                    ),
                  ),
                ],
              ),
            ),
            if (!authController.isLoggedIn)
              Padding(
                padding: const EdgeInsets.all(20.0),
                child: Center(
                  child: SizedBox(
                    width: screenWidth < 600 ? screenWidth * 0.8 : 600,
                    child: Column(
                      mainAxisSize: MainAxisSize.min,
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        Text(
                          loginAsOwner ? 'Owner Login' : 'Therapist Login',
                          style: const TextStyle(
                            fontSize: 24,
                            fontWeight: FontWeight.bold,
                          ),
                          textAlign: TextAlign.center,
                        ),
                        const SizedBox(height: 20),
                        _buildLoginForm(),
                        const SizedBox(height: 12),
                        forgotMyPasswordBtn(),
                        TextButton(
                          key: loginAsOwner
                              ? const Key('owner_register_link')
                              : const Key('therapist_register_link'),
                          onPressed: () async {
                            await Navigator.pushNamed(
                                context,
                                loginAsOwner
                                    ? OwnerRegistrationPage.RouteName
                                    : RegistrationPage.RouteName);
                          },
                          child: Text(
                            'Don\'t have an account? Click here to register for '
                            '${loginAsOwner ? 'an owner account' : 'a therapist account'}.',
                            style: const TextStyle(color: Colors.blue),
                          ),
                        ),
                        const SizedBox(
                          height: 25,
                        ),
                        Flex(
                            direction: Axis.vertical,
                            crossAxisAlignment: CrossAxisAlignment.center,
                            children: [
                              ElevatedButton(
                                  key: loginAsOwner
                                      ? const Key('login-as-owner-button')
                                      : const Key('login-as-therapist-button'),
                                  onPressed: () {
                                    setState(() {
                                      loginAsOwner = !loginAsOwner;
                                      forgotPasswordPressed = false;
                                    });
                                  },
                                  child: loginAsOwner
                                      ? const Text('Login as Therapist')
                                      : const Text('Login as Owner'))
                            ]),
                      ],
                    ),
                  ),
                ),
              ),
          ],
        ),
      ),
    );
  }

  bool isValidEmail(final String email) {
    return RegExp(r'^[\w-]+(\.[\w-]+)*@([\w-]+\.)+[\w-]+$').hasMatch(email);
  }

  Widget _buildLoginForm() {
    formKey = GlobalKey<FormState>();

    return Form(
        key: formKey,
        child: Column(mainAxisSize: MainAxisSize.min, children: [
          TextFormField(
            key: const Key('t_email_field'),
            controller: emailController,
            decoration: const InputDecoration(
              labelText: 'Email',
              border: OutlineInputBorder(),
              errorStyle: TextStyle(color: Colors.red, fontSize: 12.0),
            ),
            keyboardType: TextInputType.emailAddress,
            validator: (value) {
              // trim the email field before we validate
              value = value?.trim();
              emailController.text = emailController.text.trim();
              if (value == null || value.isEmpty) {
                return 'Email is required';
              } else if (!isValidEmail(value)) {
                return 'Please enter a valid email';
              }
              return null;
            },
            onSaved: (final value) => formData['email'] = value?.trim() ?? '',
          ),
          const SizedBox(height: 10),
          TextFormField(
            key: const Key('t_password_field'),
            controller: passwordController,
            inputFormatters: [
              LengthLimitingTextInputFormatter(20),
            ],
            decoration: InputDecoration(
              labelText: 'Password',
              border: const OutlineInputBorder(),
              suffixIcon: IconButton(
                icon: Icon(
                  passwordVisible ? Icons.visibility : Icons.visibility_off,
                  semanticLabel:
                      passwordVisible ? 'Hide password' : 'Show password',
                ),
                onPressed: () {
                  setState(() {
                    passwordVisible = !passwordVisible;
                  });
                },
              ),
            ),
            obscureText: !passwordVisible,
            validator: (final value) {
              // if the forgot password button is pressed
              // do not perform validation on this field
              if (forgotPasswordPressed) {
                return null;
              }
              if (value == null || value.isEmpty && !isPasswordNotRequired) {
                return 'Password is required';
              }
              return null;
            },
            onSaved: (final value) => formData['password'] = value ?? '',
          ),
          const SizedBox(height: 20),
          isLoading
              ? const Center(child: CircularProgressIndicator())
              : LoginButtonsWidgets()
        ]));
  }

  /// A widget containing the Login and Login as Guest Buttons
  Widget LoginButtonsWidgets() {
    return Column(
      children: [
        LoginButtonWidget(),
        const SizedBox(height: 16.0), // add space between buttons
        GuestLoginButtonWidget()
      ],
    );
  }

  /// The widget for the login button
  Widget LoginButtonWidget() {
    return ElevatedButton(
      key: const Key('login_button'),
      onPressed: isLoading
      // Disable button when loading
          ? null
          : () async {
        forgotPasswordPressed = false;

        if (formKey.currentState!.validate()) {
          formKey.currentState!.save();
          // Start loading
          setState(() {
            isLoading = true;
          });
          final authController = Provider.of<AuthController>(
              context,
              listen: false);
          final result = await authController.login(
              formData['email']!,
              formData['password']!,
              loginAsOwner ? Role.OWNER : Role.THERAPIST);

          setState(() {
            isLoading = false;
          });

          if (result != null &&
              result.containsKey('token') &&
              result.containsKey(
                  loginAsOwner ? 'ownerId' : 'therapistId')) {
            await authController.setToken(result['token']);

            if (loginAsOwner) {
              await authController
                  .setOwnerId(result['ownerId']);
            } else {
              await authController
                  .setTherapistId(result['therapistId']);
            }

            mounted
                ? Navigator.pushReplacementNamed(
                context,
                loginAsOwner
                    ? '/therapists'
                    : '/patients')
                : null;
          } else {
            mounted
                ? ScaffoldMessenger.of(context).showSnackBar(
              SnackBar(
                  content: Text(result?['message'] ??
                      'Login failed.')),
            )
                : null;
          }
        }
      },
      child: const Text('Login'),
    );
  }
  
  /// Make Login as Guest button
  Widget GuestLoginButtonWidget() {
    return ElevatedButton(
      key: const Key('guest_login_button'),
      onPressed: isLoading
      // Disable button when loading
          ? null
          : OnGuestLoginClicked,
      child: const Text('Login as Guest'),
    );
  }
  
  /// Called when the Login as Guest button is clicked
  void OnGuestLoginClicked() async {
    // Set value so form ignores password requirement
    isPasswordNotRequired = true;
      if (formKey.currentState!.validate()) {
        formKey.currentState!.save();

        // Start loading
        setState(() {
          isLoading = true;
        });
        final authController = Provider.of<AuthController>(
            context,
            listen: false);

        // Check if email is valid on the backend and log login
        final isEmailValid = await authController.loginAsGuest((formData['email']!),

        // Stop loading
        setState(() {
          isLoading = false;
        }));

        // If email is valid
        if (isEmailValid == true) {

          // Also ask for confirmation to enter guest mode
          // tell them the limits of guest mode
          final bool userAgreesToGuestMode = await showGuestModeInfoPopup();
          if (userAgreesToGuestMode) {
            // Set status of login
            mounted
                ? Navigator.pushReplacementNamed(context, '/patients')
                : null;
          }
          // If they do not agree show message about canceled login
          else {
            mounted
                ? ScaffoldMessenger.of(context).showSnackBar(
              const SnackBar(
                  content: Text('Guest Login Canceled')),
            ) : null;
            authController.logout();
          }
        }
        // Email was invalid, display message
        else {
          mounted
              ? ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(
                content: Text('Login failed. Invalid Email')),
          ) : null;
        }
      }
      // reset password not required value
      isPasswordNotRequired = false;
  }

  /// A popup describing the limits of guest mode
  Future<bool> showGuestModeInfoPopup() async {
    const String title = "Guest Mode";
    const String message = "Provides the core features for evaluating patients All data is saved locally, there is no backup in the cloud. You are responsible for safeguarding your device and data.";
    const String cancelText = "Cancel";
    const String confirmText = "Ok";

    const AlertDialog(
        title: Text(title),
        content: Text(message),
        );

    final bool? isConfirmed = await showDialog<bool>(
      context: context,
      builder: (final BuildContext context) {
        return AlertDialog(
          title: const Text(title),
          content: const Text(message),
          actions: <Widget>[
            TextButton(
              onPressed: () {
                // User cancels
                Navigator.of(context).pop(false);
              },
              child: const Text(cancelText),
            ),
            TextButton(
              onPressed: () {
                Navigator.of(context).pop(true); // User confirms
              },
              child: const Text(confirmText),
            ),
          ],
        );
      },
    );
    // Return if they confirmed or not, default to false if nothing selected
    return isConfirmed ?? false;
  }
  
}
