class Owner {
  String? ownerId;
  String email;
  String fName;
  String lName;

  Owner({
    this.ownerId,
    required this.email,
    required this.fName,
    required this.lName,
  });

  Map<String, dynamic> toJson() {
    return {'ownerId': ownerId, 'email': email, 'fName': fName, 'lName': lName};
  }

  factory Owner.fromJson(final Map<String, dynamic> json) {
    return Owner(
        ownerId: json['ownerId'] as String?,
        email: json['email'] as String,
        fName: json['fName'] as String,
        lName: json['lName'] as String);
  }
}
