import 'package:azlistview/azlistview.dart';

class Patient extends ISuspensionBean {
  String? therapistId;
  String fName;
  String lName;
  String condition;
  String phone;
  int age;
  String email;
  String doctorPhoneNumber;
  double? weight;
  double? height;
  String? guardianPhoneNumber;
  String? id;
  String? tag;
  DateTime? archivalDate;
  String? emoji;


  Patient(
      {required this.fName,
      required this.lName,
      required this.condition,
      required this.phone,
      required this.age,
      required this.email,
      required this.doctorPhoneNumber,
      this.weight,
      this.height,
      this.guardianPhoneNumber,
      this.id,
      this.archivalDate,
      this.therapistId,
      this.emoji,
      });

  Patient.fromJson(final Map<String, dynamic> json)
      : id = json['id'] ?? "",
        fName = json['fName'],
        lName = json['lName'],
        condition = json['condition'],
        phone = json['phone'],
        age = json['age'],
        email = json['email'],
        doctorPhoneNumber = json['doctorPhoneNumber'],
        weight = json['weight']?.toDouble(),
        height = json['height']?.toDouble(),
        guardianPhoneNumber = json['guardianPhoneNumber'] ?? "",
        therapistId = json['therapistID'] ??
            json['TherapistID'] ??
            "ID Failed To Get Mapped Properly",
        archivalDate = json['archivalDate'] != null
            ? DateTime.parse(json['archivalDate'] as String)
            : null,
        emoji = json['emoji'] ?? "";

  Map<String, dynamic> toJson() => {
        'id': id ?? "",
        'fName': fName,
        'lName': lName,
        'condition': condition,
        'phone': phone,
        'age': age,
        'email': email,
        'doctorPhoneNumber': doctorPhoneNumber,
        'weight': weight,
        'height': height,
        'guardianPhoneNumber': guardianPhoneNumber,
        'therapistID': therapistId,
        'archivalDate': archivalDate?.toIso8601String(),
        'emoji' : emoji,
      };

  @override
  String getSuspensionTag() {
    tag = fName.toUpperCase().substring(0, 1);
    return tag!;
  }

  // Factory method for a placeholder patient
  factory Patient.placeholderPatient() {
    return Patient(
      id: 'placeholder_id',
      fName: 'Jane',
      lName: 'Doe',
      condition: 'Sample Condition',
      phone: '555-123-4567',
      age: 30,
      email: 'jane.doe@example.com',
      doctorPhoneNumber: '555-987-6543',
      weight: 65.0,
      height: 165.0,
      guardianPhoneNumber: '555-456-7890',
      therapistId: 'placeholder_therapist_id_1',
      archivalDate: DateTime.now().subtract(const Duration(days: 30)),
    );
  }
}
