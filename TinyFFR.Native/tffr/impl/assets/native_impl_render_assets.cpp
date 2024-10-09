#include "pch.h"
#include "assets/native_impl_render_assets.h"

#include "native_impl_init.h"
#include "utils_and_constants.h"

#include "filament/filament/MaterialEnums.h"
#include "filamat/MaterialBuilder.h"

#include "filament/Engine.h"

void handle_filament_buffer_copy_callback(void* _, size_t __, BufferIdentity identity) {
	native_impl_init::deallocation_delegate(identity);
}

void native_impl_render_assets::allocate_vertex_buffer(BufferIdentity bufferIdentity, MeshVertex* vertices, int32_t vertexCount, VertexBufferHandle* outBuffer) {
	*outBuffer = VertexBuffer::Builder()
		.vertexCount(vertexCount)
		.bufferCount(1)
		.attribute(VertexAttribute::POSITION, 0, VertexBuffer::AttributeType::FLOAT3, 0, sizeof(MeshVertex))
		.attribute(VertexAttribute::UV0, 0, VertexBuffer::AttributeType::FLOAT2, 12, sizeof(MeshVertex))
		.build(*native_impl_init::filament_engine_ptr);
	(*outBuffer)->setBufferAt(*native_impl_init::filament_engine_ptr, 0, backend::BufferDescriptor{ vertices, vertexCount * sizeof(MeshVertex), &handle_filament_buffer_copy_callback, bufferIdentity });
}
StartExportedFunc(allocate_vertex_buffer, BufferIdentity bufferIdentity, native_impl_render_assets::MeshVertex* vertices, int32_t vertexCount, VertexBufferHandle* outBuffer) {
	native_impl_render_assets::allocate_vertex_buffer(bufferIdentity, vertices, vertexCount, outBuffer);
	EndExportedFunc
}

void native_impl_render_assets::allocate_index_buffer(BufferIdentity bufferIdentity, int32_t* indices, int32_t indexCount, IndexBufferHandle* outBuffer) {
	*outBuffer = IndexBuffer::Builder()
		.indexCount(indexCount)
		.bufferType(IndexBuffer::IndexType::UINT)
		.build(*native_impl_init::filament_engine_ptr);
	(*outBuffer)->setBuffer(*native_impl_init::filament_engine_ptr, IndexBuffer::BufferDescriptor{ indices, indexCount * sizeof(int32_t), &handle_filament_buffer_copy_callback, bufferIdentity });
}
StartExportedFunc(allocate_index_buffer, BufferIdentity bufferIdentity, int32_t* indices, int32_t indexCount, IndexBufferHandle* outBuffer) {
	native_impl_render_assets::allocate_index_buffer(bufferIdentity, indices, indexCount, outBuffer);
	EndExportedFunc
}

void native_impl_render_assets::dispose_vertex_buffer(VertexBufferHandle buffer) {
	if (!native_impl_init::filament_engine_ptr->destroy(buffer)) {
		Throw("Could not dispose vertex buffer.");
	}
}
StartExportedFunc(dispose_vertex_buffer, VertexBufferHandle buffer) {
	native_impl_render_assets::dispose_vertex_buffer(buffer);
	EndExportedFunc
}

void native_impl_render_assets::dispose_index_buffer(IndexBufferHandle buffer) {
	if (!native_impl_init::filament_engine_ptr->destroy(buffer)) {
		Throw("Could not dispose index buffer.");
	}
}
StartExportedFunc(dispose_index_buffer, IndexBufferHandle buffer) {
	native_impl_render_assets::dispose_index_buffer(buffer);
	EndExportedFunc
}


filamat::Package native_impl_render_assets::BasicSolidColorShaderPackage = filamat::Package::invalidPackage();

filamat::Package& GetPackage(MaterialType type) {
	switch (type) {
		case MaterialType::BasicSolidColor:
			if (!native_impl_render_assets::BasicSolidColorShaderPackage.isValid()) {
				filamat::MaterialBuilder builder;
				auto newMat = builder
					.name("Basic Solid Color")
					.require(VertexAttribute::COLOR)
					.blending(BlendingMode::OPAQUE)
					.shading(Shading::LIT)
					.culling(backend::CullingMode::BACK)
					.platform(filamat::MaterialBuilderBase::Platform::DESKTOP)
					.targetApi(filamat::MaterialBuilderBase::TargetApi::OPENGL)
					.parameter(
						"flatColor",
						backend::UniformType::FLOAT4
					)
					.material(
						"void material(inout MaterialInputs material) {"
						"	prepareMaterial(material);"
						"	material.baseColor = materialParams.flatColor;"
						"}"
					)
					.build(native_impl_init::filament_engine_ptr->getJobSystem());


				native_impl_render_assets::BasicSolidColorShaderPackage = std::move(newMat);
				if (!native_impl_render_assets::BasicSolidColorShaderPackage.isValid()) Throw("Could not create package.");
			}

			return native_impl_render_assets::BasicSolidColorShaderPackage;
		default:
			Throw("Unrecognized material type.");
	}
}

void native_impl_render_assets::create_material(MaterialType type, void* argumentsBuffer, int32_t argumentsBufferLengthBytes, MaterialHandle* outMaterial) {
	auto& package = GetPackage(type);
	auto builder = Material::Builder()
		.package(package.getData(), package.getSize());

	switch (type) {
		case MaterialType::BasicSolidColor:{
			auto mat = builder.build(*native_impl_init::filament_engine_ptr);
			mat->setDefaultParameter("flatColor", float4{ 1.0, 0.0, 0.0, 1.0 });
			*outMaterial = mat;
			break;
		}
		default:{
			Throw("Unrecognized material type.");
		}
	}
}
StartExportedFunc(create_material, MaterialType type, void* argumentsBuffer, int32_t argumentsBufferLengthBytes, MaterialHandle* outMaterial) {
	native_impl_render_assets::create_material(type, argumentsBuffer, argumentsBufferLengthBytes, outMaterial);
	EndExportedFunc
}


