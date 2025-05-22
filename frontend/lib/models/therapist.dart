import 'package:azlistview/azlistview.dart';

class Therapist extends ISuspensionBean {
  String? therapistID;
  String email;
  String fName;
  String lName;
  String? country;
  String? city;
  String? street;
  String? postalCode;
  String? phone;
  String? profession;
  String? major;
  int? yearsExperienceInHippotherapy;
  String? tag;
  String? ownerId;
  bool? verified;
  String? referral;

  Therapist({
    this.therapistID,
    required this.email,
    required this.fName,
    required this.lName,
    this.country,
    this.city,
    this.street,
    this.postalCode,
    this.phone,
    this.profession,
    this.major,
    this.yearsExperienceInHippotherapy,
    this.ownerId, // the owner id to be associated with the therapist for registration
    this.verified, //  whether or not to verify user through email
    this.referral,
  });

  /// Method to convert a Therapist to JSON
  Map<String, dynamic> toJson() {
    return {
      'therapistID': therapistID,
      'email': email,
      'fName': fName,
      'lName': lName,
      'country': country,
      'city': city,
      'street': street,
      'postalCode': postalCode,
      'phone': phone,
      'profession': profession,
      'major': major,
      'yearsExperienceInHippotherapy': yearsExperienceInHippotherapy,
      'ownerId': ownerId,
      'verified': verified,
      'referral': referral,
    };
  }

  /// Factory constructor to create a Therapist from JSON
  factory Therapist.fromJson(final Map<String, dynamic> json) {
    return Therapist(
      therapistID: json['therapistID'] as String?,
      email: json['email'] as String,
      fName: json['fName'] as String,
      lName: json['lName'] as String,
      country: json['country'] as String?,
      city: json['city'] as String?,
      street: json['street'] as String?,
      postalCode: json['postalCode'] as String?,
      phone: json['phone'] as String?,
      profession: json['profession'] as String?,
      major: json['major'] as String?,
      yearsExperienceInHippotherapy:
          json['yearsExperienceInHippotherapy'] != null
              ? int.tryParse(json['yearsExperienceInHippotherapy'].toString())
              : null,
      referral: json['referral'] as String?,
    );
  }

  /// Factory method for a placeholder therapist
  /// Factory method for a placeholder therapist with optional variation
  factory Therapist.placeholderTherapist({final int variation = 1}) {
    switch (variation) {
      case 2:
        return Therapist(
          therapistID: 'placeholder_therapist_id_2',
          email: 'alice.tailor@example.com',
          fName: 'Alice',
          lName: 'Tailor',
          country: 'USA',
          city: 'Sample City',
          street: '456 Placeholder St',
          postalCode: '12345',
          phone: '555-987-6543',
          profession: 'Hippotherapist',
          major: 'Physical Therapy',
          yearsExperienceInHippotherapy: 3,
          ownerId: 'owner1-id',
          verified: true,
          referral: 'None',
        );
      case 1:
      default:
        return Therapist(
          therapistID: 'placeholder_therapist_id_1',
          email: 'john.doe@example.com',
          fName: 'John',
          lName: 'Doe',
          country: 'USA',
          city: 'Sample City',
          street: '123 Placeholder St',
          postalCode: '12345',
          phone: '555-123-4567',
          profession: 'Hippotherapist',
          major: 'Physical Therapy',
          yearsExperienceInHippotherapy: 5,
          ownerId: 'owner1-id',
          verified: true,
          referral: 'None',
        );
    }
  }

  @override
  String getSuspensionTag() {
    tag = fName.toUpperCase().substring(0, 1);
    return tag!;
  }
}
