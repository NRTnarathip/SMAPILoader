#include "BypassAccessExceptionLib.h"
#include <dlfcn.h>
#include <sys/mman.h>
#include <cstring>
#include <array>
#include <vector>


#define LOGI(...) ((void)__android_log_print(ANDROID_LOG_INFO, "SMAPI-Tag", __VA_ARGS__))
#define LOGW(...) ((void)__android_log_print(ANDROID_LOG_WARN, "SMAPI-Tag", __VA_ARGS__))


void PatchBytes(uintptr_t targetAddress, const uint8_t* bytes, size_t bytesLength) {
	LOGI("Try patch byte at: %p", (void*)targetAddress);

	LOGI("Page size: %d", getpagesize());
	uintptr_t pageAlignedAddress = targetAddress & ~(getpagesize() - 1);
	LOGI("Page page aligned address: %p", pageAlignedAddress);

	auto error_mprotect = mprotect((void*)pageAlignedAddress, getpagesize(), PROT_READ | PROT_WRITE | PROT_EXEC);
	LOGI("result mprotect: %i", error_mprotect);

	LOGI("try write memory, length of bytes: %i", bytesLength);
	memcpy((void*)targetAddress, bytes, bytesLength);

	LOGI("Done patch byte at: %p", (void*)targetAddress);
}

extern "C" void PatchBytes(uintptr_t targetAddress, uint8_t bytes[], size_t bytesLength) {
	auto bytesPointer = reinterpret_cast<const uint8_t*>(bytes);
	PatchBytes(targetAddress, bytesPointer, bytesLength);
}


extern "C" void ApplyBypass()
{
	LOGI("Starting Apply Bypass");
	LOGI("try init");
	char AppPackageName[] = "abc.smapi.gameloader";

	LOGI("try get lib bmonosgen-2.0.so");
	void* libHandle = dlopen("libmonosgen-2.0.so", RTLD_NOW);

	LOGI("lib handle: %d", libHandle);

	{
		LOGI("try patch FieldAccessException");
		uintptr_t mono_method_can_access_field = (uintptr_t)dlsym(libHandle, "mono_method_can_access_field");
		LOGI("mono_method_can_access_field ptr: %d", mono_method_can_access_field);

		uint8_t can_access_field_patchBytes[] = { 0x20, 0x00, 0x80, 0x52 };
		PatchBytes(mono_method_can_access_field + 0x120, can_access_field_patchBytes, 4);
	}


	{
		LOGI("try patch MethodAccessException");

		uintptr_t mono_method_can_access_method = (uintptr_t)dlsym(libHandle, "mono_method_can_access_method");
		LOGI("mono_method_can_access_method: %d", mono_method_can_access_method);

		uintptr_t mono_method_can_access_method_full = mono_method_can_access_method + 0x24;
		LOGI("mono_method_can_access_method_full: %d", mono_method_can_access_method_full);

		auto targetAddress = mono_method_can_access_method_full + 0x1C;
		LOGI("targetAddress: %d", targetAddress);
		std::array<uint8_t, 20> patchBytes = {
			0x1F, 0x20, 0x03, 0xD5,
			0x1F, 0x20, 0x03, 0xD5,
			0x1F, 0x20, 0x03, 0xD5,
			0x1F, 0x20, 0x03, 0xD5,
			0x1F, 0x20, 0x03, 0xD5,
		};

		PatchBytes(targetAddress, patchBytes.data(), patchBytes.size());
	}

	LOGI("Done Apply Bypass");
}

extern "C" void ApplyBypass_x64() {
	LOGI("Starting Apply Bypass for intel x64");
	LOGI("try init");
	char AppPackageName[] = "abc.smapi.gameloader";

	LOGI("try get lib bmonosgen-2.0.so");
	void* libHandle = dlopen("libmonosgen-2.0.so", RTLD_NOW);

	LOGI("lib handle: %d", libHandle);

	{
		LOGI("try patch FieldAccessException");
		uintptr_t mono_method_can_access_field = (uintptr_t)dlsym(libHandle, "mono_method_can_access_field");
		LOGI("mono_method_can_access_field ptr: %d", mono_method_can_access_field);

		std::vector<uint8_t> patchBytes = {
			  0xB8, 0x01, 0x00, 0x00, 0x00, // MOV EAX, 1
			  0x48, 0x83, 0xC4, 0x58,       // ADD RSP, 0x58
			  0x5B,                         // POP RBX
			  0x41, 0x5C,                   // POP R12
			  0x41, 0x5D,                   // POP R13
			  0x41, 0x5E,                   // POP R14
			  0x41, 0x5F,                   // POP R15
			  0x5D,                         // POP RBP
			  0xC3
		};
		PatchBytes(mono_method_can_access_field + 0x132, patchBytes.data(), patchBytes.size());
	}


	{
		LOGI("try patch MethodAccessException");

		uintptr_t mono_method_can_access_method = (uintptr_t)dlsym(libHandle, "mono_method_can_access_method");
		LOGI("mono_method_can_access_method: %d", mono_method_can_access_method);

		uintptr_t mono_method_can_access_method_full = mono_method_can_access_method + 0x30;
		LOGI("mono_method_can_access_method_full: %d", mono_method_can_access_method_full);

		auto targetAddress = mono_method_can_access_method_full + 0x15;
		LOGI("targetAddress: %d", targetAddress);
		std::vector<uint8_t> patchBytes = {
			0xEB, 0x09, //jump to return;
		};
		PatchBytes(targetAddress, patchBytes.data(), patchBytes.size());
	}

	LOGI("Done Apply Bypass for x64");
}
