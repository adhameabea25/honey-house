import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/providers.dart';
import '../auth/auth_controller.dart';
import 'address_models.dart';

final governoratesProvider = FutureProvider<List<Governorate>>((ref) async {
  ref.watch(localeProvider);
  final data = await ref.read(apiClientProvider).get<List<dynamic>>('/governorates', auth: false);
  return data.map((e) => Governorate.fromJson(e as Map<String, dynamic>)).toList();
});

class AddressesController extends AsyncNotifier<List<Address>> {
  @override
  Future<List<Address>> build() async {
    ref.watch(localeProvider);
    if (!ref.watch(isSignedInProvider)) return [];
    final data = await ref.read(apiClientProvider).get<List<dynamic>>('/addresses');
    return data.map((e) => Address.fromJson(e as Map<String, dynamic>)).toList();
  }

  Future<Address> save(AddressInput input, {int? id}) async {
    final api = ref.read(apiClientProvider);
    final json = id == null
        ? await api.post<Map<String, dynamic>>('/addresses', data: input.toJson())
        : await api.put<Map<String, dynamic>>('/addresses/$id', data: input.toJson());
    ref.invalidateSelf();
    await future;
    return Address.fromJson(json);
  }

  Future<void> delete(int id) async {
    await ref.read(apiClientProvider).delete<void>('/addresses/$id');
    ref.invalidateSelf();
  }

  Future<void> makeDefault(int id) async {
    await ref.read(apiClientProvider).put<void>('/addresses/$id/default');
    ref.invalidateSelf();
  }
}

final addressesProvider = AsyncNotifierProvider<AddressesController, List<Address>>(AddressesController.new);
