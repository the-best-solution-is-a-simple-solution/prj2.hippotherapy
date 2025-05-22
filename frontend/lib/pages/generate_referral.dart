import 'package:flutter/material.dart';
import 'package:flutter_form_builder/flutter_form_builder.dart';
import 'package:form_builder_validators/form_builder_validators.dart';
import 'package:frontend/controllers/auth_controller.dart';
import 'package:frontend/widgets/helper_widgets/alert_dialog_box.dart';
import 'package:frontend/widgets/navigation_widgets/drawer_widget.dart';
import 'package:http/http.dart';
import 'package:provider/provider.dart';

class GenerateReferralPage extends StatefulWidget {
  static const routeName = "/generate-referral";

  const GenerateReferralPage({super.key});

  @override
  State<StatefulWidget> createState() => _GenerateReferralPageState();
}

class _GenerateReferralPageState extends State<GenerateReferralPage> {
  final _formKey = GlobalKey<FormBuilderState>();
  late final String
      ownerId; // representing the current owner who is signed in, declared in init

  // initialize the widget every time
  @override
  void initState() {
    super.initState();
    // grab ownerId from current AuthController instance
    final authController = Provider.of<AuthController>(context, listen: false);
    final checkOwner = authController.ownerId ?? '';

    if (checkOwner.isEmpty) {
      alertDialogBox(
          context, "Error: Owner is not signed in or is not of role Owner");

      // push back to login page
      return;
    }

    ownerId = checkOwner;
  }

  // handle onSubmit of the generation of referral code
  Future<void> onSubmit(final FormBuilderState? formInfo) async {
    // grab ownerId from authController and grab the value from the form
    final Response? res = await AuthController()
        .generateReferral(ownerId, formInfo?.value["email"]);

    if (res != null) {
      if (res.statusCode == 200) {
        alertDialogBox(
            context,
            "Referral successfully sent to ${formInfo?.value["email"]}.",
            "Please tell reference to check SPAM email if they don't see it in their inbox.");
      } else if (res.statusCode == 503) {
        alertDialogBox(context, "Unable to contact mail server to send email.",
            "Please try again later.");
      }
    } else {
      alertDialogBox(context, "Cannot reach Server",
          "Please try again later, unable to access server.");
    }
  }

  @override
  Widget build(final BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        backgroundColor: Theme.of(context).colorScheme.primary,
        title: const Text("Referral"),
      ),
      drawer: const HippoAppDrawer(),
      body: Center(
        child: Column(
          children: [
            Padding(
              padding: EdgeInsets.all(MediaQuery.of(context).size.width * 0.15),
              child: Center(
                child: FormBuilder(
                  key: _formKey,
                  child: Column(
                    children: [
                      Text(
                        "Please input the therapist's email that you would like to refer to your organization.",
                        textAlign: TextAlign.center,
                        textScaler: MediaQuery.textScalerOf(context),
                        style: const TextStyle(
                            fontSize: 20, fontWeight: FontWeight.w900),
                      ),
                      SizedBox(
                          height: MediaQuery.of(context).size.height * 0.03),
                      FormBuilderTextField(
                        name: "email",
                        key: const Key("referral_email"),
                        decoration: const InputDecoration(labelText: "Email"),
                        validator: FormBuilderValidators.compose([
                          FormBuilderValidators.required(
                              errorText: "Email field cannot be empty."),
                          FormBuilderValidators.email(),
                        ]),
                      ),
                      SizedBox(
                        height: MediaQuery.of(context).size.height * 0.04,
                      ),
                      ElevatedButton(
                        key: const Key("submit_referral"),
                        onPressed: () async {
                          _formKey.currentState?.saveAndValidate();
                          debugPrint(_formKey.currentState?.value.toString());

                          await onSubmit(_formKey.currentState);
                        },
                        child: const Text('Submit',
                            style: TextStyle(fontWeight: FontWeight.w900)),
                      )
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
}
