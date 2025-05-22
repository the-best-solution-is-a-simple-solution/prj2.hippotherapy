import 'package:flutter/material.dart';

/// Universal alert box to display text
/// Title - The title of the alertBox
/// Body - The body of the alertBox
/// Buttons - The list of Buttons to be added to the bottom of the AlertBox
dynamic alertDialogBox(final BuildContext context, final String title,
    [final String? body, final List<TextButton>? buttons]) {
  // returns a show dialog, can only be called in a stateless widget (must have a build method).

  return showDialog(
      context: context,
      builder: (final BuildContext context) => FittedBox(
            fit: BoxFit.scaleDown,
            child: AlertDialog(
              title: Text(
                title,
                textAlign: TextAlign.center,
              ),
              content: body != null
                  ? Text(body,
                      textAlign: TextAlign.center,
                      textScaler: const TextScaler.linear(1.5))
                  : null,
              actions: buttons ??
                  [
                    TextButton(
                        onPressed: () {
                          Navigator.of(context).pop();
                        },
                        child: const Text("OK"))
                  ],
              actionsAlignment: MainAxisAlignment.center,
            ),
          ));
}
