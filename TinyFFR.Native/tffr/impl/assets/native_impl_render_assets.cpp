#include "pch.h"
#include "assets/native_impl_render_assets.h"

#include "native_impl_init.h"
#include "utils_and_constants.h"

#include "filament/filament/MaterialEnums.h"

#include "filament/Engine.h"
#include "filament/TextureSampler.h"

static void handle_filament_buffer_copy_callback(void* _, size_t __, BufferIdentity identity) {
	native_impl_init::deallocation_delegate(identity);
}

void native_impl_render_assets::allocate_vertex_buffer(BufferIdentity bufferIdentity, MeshVertex* vertices, int32_t vertexCount, VertexBufferHandle* outBuffer) {
	ThrowIfNull(vertices, "Vertices pointer was null.");
	ThrowIfNegative(vertexCount, "Vertex count was negative.");
	ThrowIfNull(outBuffer, "Out buffer pointer was null.");
	*outBuffer = VertexBuffer::Builder()
		.vertexCount(vertexCount)
		.bufferCount(1)
		.attribute(VertexAttribute::POSITION, 0, VertexBuffer::AttributeType::FLOAT3, 0, sizeof(MeshVertex))
		.attribute(VertexAttribute::UV0, 0, VertexBuffer::AttributeType::FLOAT2, 12, sizeof(MeshVertex))
		.attribute(VertexAttribute::TANGENTS, 0, VertexBuffer::AttributeType::FLOAT4, 20, sizeof(MeshVertex))
		.build(*filament_engine);
	(*outBuffer)->setBufferAt(*filament_engine, 0, backend::BufferDescriptor{ vertices, vertexCount * sizeof(MeshVertex), &handle_filament_buffer_copy_callback, bufferIdentity });
}
StartExportedFunc(allocate_vertex_buffer, BufferIdentity bufferIdentity, native_impl_render_assets::MeshVertex* vertices, int32_t vertexCount, VertexBufferHandle* outBuffer) {
	native_impl_render_assets::allocate_vertex_buffer(bufferIdentity, vertices, vertexCount, outBuffer);
	EndExportedFunc
}

void native_impl_render_assets::allocate_index_buffer(BufferIdentity bufferIdentity, int32_t* indices, int32_t indexCount, IndexBufferHandle* outBuffer) {
	ThrowIfNull(indices, "Indices pointer was null.");
	ThrowIfNegative(indexCount, "Index count was negative.");
	ThrowIfNull(outBuffer, "Out buffer pointer was null.");
	*outBuffer = IndexBuffer::Builder()
		.indexCount(indexCount)
		.bufferType(IndexBuffer::IndexType::UINT)
		.build(*filament_engine);
	(*outBuffer)->setBuffer(*filament_engine, IndexBuffer::BufferDescriptor{ indices, indexCount * sizeof(int32_t), &handle_filament_buffer_copy_callback, bufferIdentity });
}
StartExportedFunc(allocate_index_buffer, BufferIdentity bufferIdentity, int32_t* indices, int32_t indexCount, IndexBufferHandle* outBuffer) {
	native_impl_render_assets::allocate_index_buffer(bufferIdentity, indices, indexCount, outBuffer);
	EndExportedFunc
}

void native_impl_render_assets::dispose_vertex_buffer(VertexBufferHandle buffer) {
	ThrowIfNull(buffer, "Buffer was null.");
	if (!filament_engine->destroy(buffer)) {
		Throw("Could not dispose vertex buffer.");
	}
}
StartExportedFunc(dispose_vertex_buffer, VertexBufferHandle buffer) {
	native_impl_render_assets::dispose_vertex_buffer(buffer);
	EndExportedFunc
}

void native_impl_render_assets::dispose_index_buffer(IndexBufferHandle buffer) {
	ThrowIfNull(buffer, "Buffer was null.");
	if (!filament_engine->destroy(buffer)) {
		Throw("Could not dispose index buffer.");
	}
}
StartExportedFunc(dispose_index_buffer, IndexBufferHandle buffer) {
	native_impl_render_assets::dispose_index_buffer(buffer);
	EndExportedFunc
}

void native_impl_render_assets::calculate_tangent_rotation(float3 tangent, float3 bitangent, float3 normal, float4* outRotation) {
	ThrowIfNull(outRotation, "Out rotation pointer was null.");
	*outRotation = mat3f::packTangentFrame({ tangent, bitangent, normal }).xyzw;
}
StartExportedFunc(calculate_tangent_rotation, float3 tangent, float3 bitangent, float3 normal, float4* outRotation) {
	native_impl_render_assets::calculate_tangent_rotation(tangent, bitangent, normal, outRotation);
	EndExportedFunc
}

void native_impl_render_assets::load_texture_rgb_24(BufferIdentity bufferIdentity, void* dataPtr, int32_t dataLen, uint32_t width, uint32_t height, interop_bool generateMipMaps, interop_bool isLinearColorspace, TextureHandle* outTexture) {
	ThrowIfNull(dataPtr, "Data pointer was null.");
	ThrowIfNegative(dataLen, "Data length was negative.");
	ThrowIfNull(outTexture, "Out texture pointer was null.");

	Texture::PixelBufferDescriptor imageBuffer {
		dataPtr,
		static_cast<size_t>(dataLen),
		backend::PixelDataFormat::RGB, // Specifies how the texels will "appear" to the shader
		backend::PixelDataType::UBYTE, // Specifies how the texels are stored in the dataPtr
		1, 0, 0, 0,
		&handle_filament_buffer_copy_callback,
		bufferIdentity
	};

	*outTexture = Texture::Builder()
		.depth(1)
		.format(isLinearColorspace ? Texture::InternalFormat::RGB8 : Texture::InternalFormat::SRGB8) // Specifies the format the data is stored as internally on the GPU
		.height(height)
		.levels(generateMipMaps ? 0xFF : 1)
		.sampler(Texture::Sampler::SAMPLER_2D)
		.usage(Texture::Usage::DEFAULT)
		.width(width)
		.build(*filament_engine);
	ThrowIfNull(*outTexture, "Could not load texture.");

	(*outTexture)->setImage(*filament_engine, 0, std::move(imageBuffer));
	if (!generateMipMaps) return;
	(*outTexture)->generateMipmaps(*filament_engine);
}
StartExportedFunc(load_texture_rgb_24, BufferIdentity bufferIdentity, void* dataPtr, int32_t dataLen, uint32_t width, uint32_t height, interop_bool generateMipMaps, interop_bool isLinearColorspace, TextureHandle* outTexture) {
	native_impl_render_assets::load_texture_rgb_24(bufferIdentity, dataPtr, dataLen, width, height, generateMipMaps, isLinearColorspace, outTexture);
	EndExportedFunc
}

void native_impl_render_assets::load_texture_rgba_32(BufferIdentity bufferIdentity, void* dataPtr, int32_t dataLen, uint32_t width, uint32_t height, interop_bool generateMipMaps, interop_bool isLinearColorspace, TextureHandle* outTexture) {
	ThrowIfNull(dataPtr, "Data pointer was null.");
	ThrowIfNegative(dataLen, "Data length was negative.");
	ThrowIfNull(outTexture, "Out texture pointer was null.");

	Texture::PixelBufferDescriptor imageBuffer{
		dataPtr,
		static_cast<size_t>(dataLen),
		backend::PixelDataFormat::RGBA,
		backend::PixelDataType::UBYTE,
		1, 0, 0, 0,
		&handle_filament_buffer_copy_callback,
		bufferIdentity
	};

	*outTexture = Texture::Builder()
		.depth(1)
		.format(isLinearColorspace ? Texture::InternalFormat::RGBA8 : Texture::InternalFormat::SRGB8_A8)
		.height(height)
		.levels(generateMipMaps ? 0xFF : 1)
		.sampler(Texture::Sampler::SAMPLER_2D)
		.usage(Texture::Usage::DEFAULT)
		.width(width)
		.build(*filament_engine);
	ThrowIfNull(*outTexture, "Could not load texture.");

	(*outTexture)->setImage(*filament_engine, 0, std::move(imageBuffer));
	if (!generateMipMaps) return;
	(*outTexture)->generateMipmaps(*filament_engine);
}
StartExportedFunc(load_texture_rgba_32, BufferIdentity bufferIdentity, void* dataPtr, int32_t dataLen, uint32_t width, uint32_t height, interop_bool generateMipMaps, interop_bool isLinearColorspace, TextureHandle* outTexture) {
	native_impl_render_assets::load_texture_rgba_32(bufferIdentity, dataPtr, dataLen, width, height, generateMipMaps, isLinearColorspace, outTexture);
	EndExportedFunc
}

void native_impl_render_assets::dispose_texture(TextureHandle texture) {
	ThrowIfNull(texture, "Texture was null.");
	filament_engine->destroy(texture);
}
StartExportedFunc(dispose_texture, TextureHandle texture) {
	native_impl_render_assets::dispose_texture(texture);
	EndExportedFunc
}

void native_impl_render_assets::load_shader_package(void* dataPtr, int32_t dataLen, PackageHandle* outHandle) {
	ThrowIfNull(dataPtr, "Data pointer was null");
	ThrowIfNegative(dataLen, "Data length was negative.");
	ThrowIfNull(outHandle, "Out handle pointer was null.");
	*outHandle = Material::Builder().package(dataPtr, static_cast<size_t>(dataLen)).build(*filament_engine);
	ThrowIfNull(*outHandle, "Could not create material package.");
}
StartExportedFunc(load_shader_package, void* dataPtr, int32_t dataLen, PackageHandle* outHandle) {
	native_impl_render_assets::load_shader_package(dataPtr, dataLen, outHandle);
	EndExportedFunc
}

void native_impl_render_assets::create_material(PackageHandle package, MaterialHandle* outMaterial) {
	ThrowIfNull(package, "Package handle was null.");
	ThrowIfNull(outMaterial, "Out material pointer was null.");
	*outMaterial = package->createInstance();
	ThrowIfNull(*outMaterial, "Could not create material.");
}
StartExportedFunc(create_material, PackageHandle package, MaterialHandle* outMaterial) {
	native_impl_render_assets::create_material(package, outMaterial);
	EndExportedFunc
}

void native_impl_render_assets::duplicate_material(MaterialHandle targetMaterial, MaterialHandle* outMaterial) {
	ThrowIfNull(targetMaterial, "Package handle was null.");
	ThrowIfNull(outMaterial, "Out material pointer was null.");
	*outMaterial = MaterialInstance::duplicate(targetMaterial);
	ThrowIfNull(*outMaterial, "Could not create material.");
}
StartExportedFunc(duplicate_material, MaterialHandle targetMaterial, MaterialHandle* outMaterial) {
	native_impl_render_assets::duplicate_material(targetMaterial, outMaterial);
	EndExportedFunc
}

void native_impl_render_assets::set_material_parameter_texture(MaterialHandle material, const char* parameterName, int32_t parameterNameLength, TextureHandle texture) {
	ThrowIfNull(material, "Material was null.");
	ThrowIfNull(parameterName, "Parameter name was null.");
	ThrowIfNegative(parameterNameLength, "Parameter name length was negative.");
	ThrowIfNull(texture, "Texture was null.");

	TextureSampler sampler {
		backend::SamplerMagFilter::LINEAR,
		backend::SamplerWrapMode::REPEAT
	};
	sampler.setAnisotropy(4.0f);
	material->setParameter(parameterName, static_cast<size_t>(parameterNameLength), texture, sampler);
}
StartExportedFunc(set_material_parameter_texture, MaterialHandle material, const char* parameterName, int32_t parameterNameLength, TextureHandle texture) {
	native_impl_render_assets::set_material_parameter_texture(material, parameterName, parameterNameLength, texture);
	EndExportedFunc
}
void native_impl_render_assets::set_material_parameter_real(MaterialHandle material, const char* parameterName, int32_t parameterNameLength, float val) {
	ThrowIfNull(material, "Material was null.");
	ThrowIfNull(parameterName, "Parameter name was null.");
	ThrowIfNegative(parameterNameLength, "Parameter name length was negative.");

	material->setParameter(parameterName, static_cast<size_t>(parameterNameLength), val);
}
StartExportedFunc(set_material_parameter_real, MaterialHandle material, const char* parameterName, int32_t parameterNameLength, float val) {
	native_impl_render_assets::set_material_parameter_real(material, parameterName, parameterNameLength, val);
	EndExportedFunc
}

void native_impl_render_assets::set_material_parameter_matrix(MaterialHandle material, const char* parameterName, int32_t parameterNameLength, mat4f& valRef) {
	ThrowIfNull(material, "Material was null.");
	ThrowIfNull(parameterName, "Parameter name was null.");
	ThrowIfNegative(parameterNameLength, "Parameter name length was negative.");

	material->setParameter(parameterName, static_cast<size_t>(parameterNameLength), valRef);
}
StartExportedFunc(set_material_parameter_matrix, MaterialHandle material, const char* parameterName, int32_t parameterNameLength, mat4f& valRef) {
	native_impl_render_assets::set_material_parameter_matrix(material, parameterName, parameterNameLength, valRef);
	EndExportedFunc
}

void native_impl_render_assets::dispose_material(MaterialHandle material) {
	ThrowIfNull(material, "Material was null.");
	filament_engine->destroy(material);
}
StartExportedFunc(dispose_material, MaterialHandle material) {
	native_impl_render_assets::dispose_material(material);
	EndExportedFunc
}

void native_impl_render_assets::dispose_shader_package(PackageHandle handle) {
	filament_engine->destroy(handle);
}
StartExportedFunc(dispose_shader_package, PackageHandle handle) {
	native_impl_render_assets::dispose_shader_package(handle);
	EndExportedFunc
}
