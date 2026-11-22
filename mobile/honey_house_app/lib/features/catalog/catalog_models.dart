import '../../core/json.dart';

class Category {
  Category({required this.id, required this.name, required this.slug, this.description, this.imageUrl, this.subCategories = const []});
  final int id;
  final String name;
  final String slug;
  final String? description;
  final String? imageUrl;
  final List<Category> subCategories;

  factory Category.fromJson(Map<String, dynamic> j) => Category(
        id: j['id'] as int,
        name: j['name'] as String? ?? '',
        slug: j['slug'] as String? ?? '',
        description: j['description'] as String?,
        imageUrl: j['imageUrl'] as String?,
        subCategories: listOf(j['subCategories'], Category.fromJson),
      );
}

class CategoryRef {
  CategoryRef(this.id, this.name);
  final int id;
  final String name;
  factory CategoryRef.fromJson(Map<String, dynamic> j) => CategoryRef(j['id'] as int, j['name'] as String? ?? '');
}

class ProductSummary {
  ProductSummary({required this.id, required this.name, required this.category, this.mainImageUrl, required this.minPrice,
      required this.maxPrice, this.compareAtPrice, required this.currency, required this.averageRating, required this.reviewCount,
      required this.isFeatured, required this.inStock});
  final int id;
  final String name;
  final CategoryRef category;
  final String? mainImageUrl;
  final double minPrice;
  final double maxPrice;
  final double? compareAtPrice;
  final String currency;
  final double averageRating;
  final int reviewCount;
  final bool isFeatured;
  final bool inStock;

  factory ProductSummary.fromJson(Map<String, dynamic> j) => ProductSummary(
        id: j['id'] as int,
        name: j['name'] as String? ?? '',
        category: CategoryRef.fromJson(j['category'] as Map<String, dynamic>),
        mainImageUrl: j['mainImageUrl'] as String?,
        minPrice: toDouble(j['minPrice']),
        maxPrice: toDouble(j['maxPrice']),
        compareAtPrice: toDoubleOrNull(j['compareAtPrice']),
        currency: j['currency'] as String? ?? 'EGP',
        averageRating: toDouble(j['averageRating']),
        reviewCount: j['reviewCount'] as int? ?? 0,
        isFeatured: j['isFeatured'] as bool? ?? false,
        inStock: j['inStock'] as bool? ?? false,
      );
}

class Variant {
  Variant({required this.id, required this.weightGrams, required this.label, required this.price, this.compareAtPrice,
      required this.inStock, required this.availableQuantity});
  final int id;
  final int weightGrams;
  final String label;
  final double price;
  final double? compareAtPrice;
  final bool inStock;
  final int availableQuantity;

  factory Variant.fromJson(Map<String, dynamic> j) => Variant(
        id: j['id'] as int,
        weightGrams: j['weightGrams'] as int,
        label: j['label'] as String? ?? '',
        price: toDouble(j['price']),
        compareAtPrice: toDoubleOrNull(j['compareAtPrice']),
        inStock: j['inStock'] as bool? ?? false,
        availableQuantity: j['availableQuantity'] as int? ?? 0,
      );
}

class ProductImage {
  ProductImage(this.url, this.isMain);
  final String url;
  final bool isMain;
  factory ProductImage.fromJson(Map<String, dynamic> j) => ProductImage(j['url'] as String, j['isMain'] as bool? ?? false);
}

class ProductDetail {
  ProductDetail({required this.id, required this.name, this.description, this.origin, required this.category, required this.images,
      required this.variants, required this.currency, required this.averageRating, required this.reviewCount});
  final int id;
  final String name;
  final String? description;
  final String? origin;
  final CategoryRef category;
  final List<ProductImage> images;
  final List<Variant> variants;
  final String currency;
  final double averageRating;
  final int reviewCount;

  factory ProductDetail.fromJson(Map<String, dynamic> j) => ProductDetail(
        id: j['id'] as int,
        name: j['name'] as String? ?? '',
        description: j['description'] as String?,
        origin: j['origin'] as String?,
        category: CategoryRef.fromJson(j['category'] as Map<String, dynamic>),
        images: listOf(j['images'], ProductImage.fromJson),
        variants: listOf(j['variants'], Variant.fromJson),
        currency: j['currency'] as String? ?? 'EGP',
        averageRating: toDouble(j['averageRating']),
        reviewCount: j['reviewCount'] as int? ?? 0,
      );
}

class Review {
  Review({required this.id, required this.rating, this.comment, required this.reviewerName, required this.isVerifiedPurchase, required this.createdAt});
  final int id;
  final int rating;
  final String? comment;
  final String reviewerName;
  final bool isVerifiedPurchase;
  final DateTime createdAt;

  factory Review.fromJson(Map<String, dynamic> j) => Review(
        id: j['id'] as int,
        rating: j['rating'] as int,
        comment: j['comment'] as String?,
        reviewerName: j['reviewerName'] as String? ?? '',
        isVerifiedPurchase: j['isVerifiedPurchase'] as bool? ?? false,
        createdAt: parseUtc(j['createdAt']),
      );
}

class ProductReviews {
  ProductReviews(this.average, this.count, this.distribution, this.reviews);
  final double average;
  final int count;
  final Map<int, int> distribution;
  final Paged<Review> reviews;

  factory ProductReviews.fromJson(Map<String, dynamic> j) {
    final s = j['summary'] as Map<String, dynamic>;
    final dist = <int, int>{};
    (s['distribution'] as Map? ?? {}).forEach((k, v) => dist[int.parse(k.toString())] = v as int);
    return ProductReviews(toDouble(s['average']), s['count'] as int, dist,
        Paged.fromJson(j['reviews'] as Map<String, dynamic>, Review.fromJson));
  }
}

class MyReview {
  MyReview({required this.id, required this.rating, this.comment, required this.isApproved});
  final int id;
  final int rating;
  final String? comment;
  final bool isApproved;
  factory MyReview.fromJson(Map<String, dynamic> j) =>
      MyReview(id: j['id'] as int, rating: j['rating'] as int, comment: j['comment'] as String?, isApproved: j['isApproved'] as bool? ?? false);
}

enum ProductSort {
  newest('newest'),
  priceAsc('price_asc'),
  priceDesc('price_desc'),
  rating('rating'),
  bestselling('bestselling');

  const ProductSort(this.api);
  final String api;
}

class ProductFilter {
  const ProductFilter({this.search, this.categoryId, this.minPrice, this.maxPrice, this.inStock = false, this.sort = ProductSort.newest});
  final String? search;
  final int? categoryId;
  final double? minPrice;
  final double? maxPrice;
  final bool inStock;
  final ProductSort sort;

  ProductFilter copyWith({String? search, int? categoryId, double? minPrice, double? maxPrice, bool? inStock, ProductSort? sort,
          bool clearCategory = false, bool clearPrices = false}) =>
      ProductFilter(
        search: search ?? this.search,
        categoryId: clearCategory ? null : categoryId ?? this.categoryId,
        minPrice: clearPrices ? null : minPrice ?? this.minPrice,
        maxPrice: clearPrices ? null : maxPrice ?? this.maxPrice,
        inStock: inStock ?? this.inStock,
        sort: sort ?? this.sort,
      );

  Map<String, dynamic> toQuery(int page) => {
        'Search': search,
        'CategoryId': categoryId,
        'MinPrice': minPrice,
        'MaxPrice': maxPrice,
        'InStock': inStock ? true : null,
        'Sort': sort.api,
        'Page': page,
        'PageSize': 20,
      };

  @override
  bool operator ==(Object other) =>
      other is ProductFilter && other.search == search && other.categoryId == categoryId && other.minPrice == minPrice &&
      other.maxPrice == maxPrice && other.inStock == inStock && other.sort == sort;

  @override
  int get hashCode => Object.hash(search, categoryId, minPrice, maxPrice, inStock, sort);
}
